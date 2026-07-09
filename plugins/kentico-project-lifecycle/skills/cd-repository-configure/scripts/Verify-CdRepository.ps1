<#
.SYNOPSIS
Verifies that a generated CD repository matches the deployment scope defined in repository.config.

.DESCRIPTION
Runs after the CD store operation (kxp-cd-store, typically invoked by Export-DeploymentPackage.ps1)
has generated the CD repository folder. Compares the filters in repository.config against the
serialized XML files actually present in the repository and reports:

- [FAIL] Included code names or content item names with no corresponding serialized file
         (catches silent suppression, e.g. a content type missing from ObjectFilters).
- [FAIL] A content type listed in IncludedContentItemsOfType that is missing from
         ObjectFilters/IncludedCodeNames ObjectType="cms.contenttype" (when that filter is used) -
         kxp-cd-store silently drops all content items of that type.
- [WARN] Included object types / content types with no serialized files, IncludeAll usage,
         and a destructive 'Full' RestoreMode.

Exit codes: 0 = no failures, 1 = failures found, 2 = invalid input.

.EXAMPLE
./Verify-CdRepository.ps1 -RepositoryPath "C:\my-app\App_Data\CDRepository"
#>
[CmdletBinding()]
param(
    # Folder containing the generated CD repository (the folder that holds repository.config).
    [Parameter(Mandatory = $true)]
    [string]$RepositoryPath,

    # Path to repository.config. Defaults to <RepositoryPath>\repository.config.
    [string]$ConfigPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $RepositoryPath -PathType Container)) {
    Write-Host "[FAIL] CD repository folder not found: $RepositoryPath"
    exit 2
}
$RepositoryPath = (Resolve-Path $RepositoryPath).Path
if (-not $ConfigPath) { $ConfigPath = Join-Path $RepositoryPath 'repository.config' }
if (-not (Test-Path $ConfigPath -PathType Leaf)) {
    Write-Host "[FAIL] repository.config not found: $ConfigPath"
    exit 2
}

$failures = [System.Collections.Generic.List[string]]::new()
$warnings = [System.Collections.Generic.List[string]]::new()
$passes   = [System.Collections.Generic.List[string]]::new()

try {
    [xml]$config = Get-Content -Path $ConfigPath -Raw
}
catch {
    Write-Host "[FAIL] repository.config is not well-formed XML: $($_.Exception.Message)"
    exit 1
}
$root = $config.DocumentElement

# Index the repository content once.
$allFiles = Get-ChildItem -Path $RepositoryPath -Recurse -File -Filter '*.xml' |
    Where-Object { $_.FullName -ne (Resolve-Path $ConfigPath).Path }
$allDirs = Get-ChildItem -Path $RepositoryPath -Recurse -Directory

function ConvertTo-WildcardPattern([string]$codeNamePattern) {
    # repository.config uses % as the wildcard; PowerShell -like uses *.
    return ($codeNamePattern.Trim() -replace '%', '*')
}

function Get-FilesUnderTypeFolder([string]$objectType) {
    # Serialized files live under a folder named after the object type,
    # inside @global or a channel folder (possibly nested, e.g. parent-scoped child objects).
    $allFiles | Where-Object { ($_.DirectoryName -split '[\\/]') -contains $objectType }
}

function Get-RelativePath([string]$fullPath) {
    return $fullPath.Substring($RepositoryPath.Length).TrimStart('\', '/')
}

# --- RestoreMode ---
$restoreModeNode = $root.SelectSingleNode('RestoreMode')
if ($null -eq $restoreModeNode) {
    $failures.Add('RestoreMode element is missing - CD deployment configurations require an explicit RestoreMode.')
}
else {
    $restoreMode = $restoreModeNode.InnerText.Trim()
    if ($restoreMode -ieq 'Full') {
        $warnings.Add("RestoreMode is 'Full' - DESTRUCTIVE: the restore deletes every object on the target that is not present in this (scoped) repository, including system objects that are not properly excluded. Use only when explicitly intended.")
    }
    else {
        $passes.Add("RestoreMode is '$restoreMode'.")
    }
}

# --- ObjectFilters/IncludedCodeNames: every included code name must have a serialized file ---
foreach ($node in $root.SelectNodes('ObjectFilters/IncludedCodeNames')) {
    $objectType = $node.GetAttribute('ObjectType')
    if (-not $objectType) {
        $warnings.Add("IncludedCodeNames without an ObjectType attribute ('$($node.InnerText.Trim())') applies to all types and was not individually verified.")
        continue
    }
    $candidates = @(Get-FilesUnderTypeFolder $objectType)
    $names = $node.InnerText -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
    foreach ($name in $names) {
        $pattern = ConvertTo-WildcardPattern $name
        $match = $candidates | Where-Object { $_.BaseName -like $pattern } | Select-Object -First 1
        if ($match) {
            $passes.Add("ObjectFilters ${objectType}: '$name' -> $(Get-RelativePath $match.FullName)")
        }
        else {
            $failures.Add("ObjectFilters ${objectType}: no serialized file found for included code name '$name'. The object may have been silently suppressed - check that its dependencies (e.g. parent types, cms.resource allowlists) are included.")
        }
    }
}

# --- ContentItemFilters/IncludedContentItemNames: every included item must be serialized ---
$contentItemNodes = $root.SelectNodes('ContentItemFilters/IncludedContentItemNames')
if ($contentItemNodes.Count -gt 0) {
    # Canonical code names come from the <ContentItemName> element inside cms.contentitem files;
    # fall back to the file base name when the element cannot be read.
    $serializedNames = foreach ($file in (Get-FilesUnderTypeFolder 'cms.contentitem')) {
        $name = $null
        try {
            [xml]$itemXml = Get-Content -Path $file.FullName -Raw
            $nameNode = $itemXml.SelectSingleNode('//ContentItemName')
            if ($nameNode) { $name = $nameNode.InnerText.Trim() }
        }
        catch { }
        if ($name) { $name } else { $file.BaseName }
    }
    foreach ($node in $contentItemNodes) {
        $rawName = $node.InnerText.Trim()
        $pattern = ConvertTo-WildcardPattern $rawName
        if ($serializedNames | Where-Object { $_ -like $pattern } | Select-Object -First 1) {
            $passes.Add("ContentItemFilters: '$rawName' matched a serialized content item.")
        }
        else {
            $failures.Add("ContentItemFilters: no serialized content item matches '$rawName'. Check that the item's content type is listed in BOTH IncludedContentItemsOfType and ObjectFilters/IncludedCodeNames ObjectType=""cms.contenttype"".")
        }
    }
}

# --- IncludedContentItemsOfType vs ObjectFilters cms.contenttype: config-level rule, not symptom-based ---
# When an ObjectFilters allowlist for cms.contenttype exists, every content type deployed via
# IncludedContentItemsOfType must also be in that allowlist, or kxp-cd-store silently drops all of
# its content items. Checked directly against the config so it fires even without ContentItemFilters
# and even before any store operation has run.
$contentTypeFilterNodes = $root.SelectNodes("ObjectFilters/IncludedCodeNames[@ObjectType='cms.contenttype']")
if ($contentTypeFilterNodes.Count -gt 0) {
    $allowedContentTypePatterns = @(
        foreach ($node in $contentTypeFilterNodes) {
            $node.InnerText -split ';' | ForEach-Object { ConvertTo-WildcardPattern $_ } | Where-Object { $_ }
        }
    )
    foreach ($node in $root.SelectNodes('IncludedContentItemsOfType/ContentType')) {
        $type = $node.InnerText.Trim()
        if ($type -eq '##WebPageFolders##') { continue }
        if ($allowedContentTypePatterns | Where-Object { $type -like $_ } | Select-Object -First 1) {
            $passes.Add("IncludedContentItemsOfType: '$type' is covered by ObjectFilters/IncludedCodeNames (cms.contenttype).")
        }
        else {
            $failures.Add("IncludedContentItemsOfType: '$type' is missing from ObjectFilters/IncludedCodeNames ObjectType=""cms.contenttype"" - kxp-cd-store will silently exclude all content items of this type from the deployment package. Add '$type' to the cms.contenttype code name list.")
        }
    }
}

# --- IncludedContentItemsOfType: each type should have serialized item data (warning only) ---
foreach ($node in $root.SelectNodes('IncludedContentItemsOfType/ContentType')) {
    $type = $node.InnerText.Trim()
    if ($type -eq '##WebPageFolders##') { continue }
    $folderName = "contentitemdata.$type"
    if ($allDirs | Where-Object { $_.Name -ieq $folderName } | Select-Object -First 1) {
        $passes.Add("IncludedContentItemsOfType: '$type' has serialized item data ($folderName).")
    }
    else {
        $warnings.Add("IncludedContentItemsOfType: no '$folderName' folder in the repository - fine if no items of '$type' were meant to deploy, otherwise its items are missing.")
    }
}
if ($root.SelectSingleNode('IncludedContentItemsOfType/IncludeAll')) {
    $warnings.Add('IncludedContentItemsOfType uses IncludeAll - not recommended for scoped CD deployments (the restore iterates ALL items of ALL types on the target).')
}

# --- IncludedObjectTypes: each type should have a folder in the repository (warning only) ---
foreach ($node in $root.SelectNodes('IncludedObjectTypes/ObjectType')) {
    $type = $node.InnerText.Trim()
    if ($allDirs | Where-Object { $_.Name -ieq $type } | Select-Object -First 1) {
        $passes.Add("IncludedObjectTypes: '$type' folder present.")
    }
    else {
        $warnings.Add("IncludedObjectTypes: no '$type' folder in the repository - fine if no objects of this type changed, otherwise expected objects are missing.")
    }
}
if ($root.SelectSingleNode('IncludedObjectTypes/IncludeAll')) {
    $warnings.Add('IncludedObjectTypes uses IncludeAll - the configuration is not minimally scoped.')
}

# --- Report ---
Write-Host ''
Write-Host '=== CD repository verification ==='
Write-Host "Config:     $ConfigPath"
Write-Host "Repository: $RepositoryPath"
Write-Host ''
foreach ($m in $passes)   { Write-Host "[ OK ] $m" }
foreach ($m in $warnings) { Write-Host "[WARN] $m" }
foreach ($m in $failures) { Write-Host "[FAIL] $m" }
Write-Host ''
Write-Host ("Summary: {0} passed, {1} warning(s), {2} failure(s)" -f $passes.Count, $warnings.Count, $failures.Count)

if ($failures.Count -gt 0) { exit 1 } else { exit 0 }

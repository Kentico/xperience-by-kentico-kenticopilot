<#
.SYNOPSIS
    Parses Kentico Migration Tool protocol and console logs into a structured YAML summary.

.DESCRIPTION
    Reads all log files produced by the Kentico Migration Tool (protocol logs + console logs)
    from a log directory, extracts structured data (command lifecycle, success/skip/fail entries,
    media counts, form results), merges results across multiple runs (re-runs override earlier
    results per entity), and writes a YAML summary.

    The YAML output is designed for AI consumption — simpler than JSON, more structured than raw text.

.PARAMETER LogDir
    Directory containing protocol*.txt and migration-run*.log files.
    All matching files are processed chronologically. Later runs override earlier results per entity.

.PARAMETER OutputPath
    Path for the output YAML file. Default: <LogDir>/parsed-log-summary.yaml

.PARAMETER WorkspaceRoot
    Workspace root directory (for relative path display). Default: current directory.

.EXAMPLE
    .\parse-migration-logs.ps1 -LogDir C:\migration\MigrationProtocol
    .\parse-migration-logs.ps1 -LogDir C:\migration\MigrationProtocol -OutputPath C:\migration\summary.yaml
#>

param(
    [string]$LogDir,
    [string]$OutputPath,
    [string]$WorkspaceRoot = (Get-Location).Path
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# --- Helper: YAML-safe string escaping ---
function Format-YamlString {
    param([string]$Value)
    if ([string]::IsNullOrEmpty($Value)) { return '""' }
    # Quote if contains special chars
    if ($Value -match '[:#\[\]{}&*!|>''"%@`\r\n]' -or $Value -match '^\s' -or $Value -match '\s$') {
        $escaped = $Value -replace '"', '\"'
        return "`"$escaped`""
    }
    return $Value
}

# --- Auto-discover log files ---
function Find-AllProtocolLogs {
    param([string]$Dir)
    Get-ChildItem -Path $Dir -Filter "protocol*.txt" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime
}

function Find-AllConsoleLogs {
    param([string]$Dir)
    Get-ChildItem -Path $Dir -Filter "migration-run*.log" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime
}

# --- Protocol Log Parsing (single file) ---
function Parse-ProtocolLog {
    param([string]$Path)

    $result = @{
        successByGuid = @{}   # guid → {type, id, name}
        skipped = [System.Collections.Generic.List[object]]::new()
        failedByGuid  = @{}   # guid → {reason, entity, nodeAliasPath, exception}
    }

    if (-not $Path -or -not (Test-Path $Path)) {
        return $result
    }

    $content = Get-Content -Path $Path -Raw -Encoding UTF8

    # Split entries by timestamp at line start
    $entries = [regex]::Split($content, '(?m)(?=^\d{8}_\d{6}: )')
    $entries = $entries | Where-Object { $_.Trim() -ne '' }

    foreach ($entry in $entries) {
        $entry = $entry.Trim()

        # Success: EntityType(ID=N, Guid=GUID Name=NAME)
        if ($entry -match '^\d{8}_\d{6}: Success: (\w+)\(ID=(\d+),\s*(?:Guid|GUID)=([0-9a-f-]+),?\s*Name=(.*)\)\s*$') {
            $guid = $Matches[3]
            $result.successByGuid[$guid] = @{
                type = $Matches[1]
                id   = $Matches[2]
                guid = $guid
                name = $Matches[4]
            }
            # Entity succeeded — remove from failed if it was there from a previous parse
            $result.failedByGuid.Remove($guid) | Out-Null
        }
        # Success: UserRoleInfo (multi-line JSON)
        elseif ($entry -match '^\d{8}_\d{6}: Success: UserRoleInfo\(') {
            # Track with a synthetic key — no GUID dedup needed
            $syntheticGuid = "userrole-" + $result.successByGuid.Keys.Where({ $_ -like 'userrole-*' }).Count
            $result.successByGuid[$syntheticGuid] = @{
                type = "UserRoleInfo"
                id   = ""
                guid = $syntheticGuid
                name = ""
            }
        }
        # NeedManualAction: False (Skipped)
        elseif ($entry -match '^\d{8}_\d{6}: NeedManualAction: False, ReferenceName: (\w+)') {
            $reason = $Matches[1]
            $entityType = ""
            if ($entry -match 'Type: (\w+)') { $entityType = $Matches[1] }
            elseif ($entry -match 'Entity: (\w+)\(') { $entityType = $Matches[1] }

            $existing = $result.skipped | Where-Object { $_.reason -eq $reason -and $_.entity -eq $entityType }
            if ($existing) {
                $existing.count++
            } else {
                $result.skipped.Add(@{
                    reason = $reason
                    entity = $entityType
                    count  = 1
                })
            }
        }
        # NeedManualAction: True (Failed)
        elseif ($entry -match '^\d{8}_\d{6}: NeedManualAction: True, ReferenceName: (\w+)') {
            $reason = $Matches[1]
            $exception = ""
            $nodeAliasPath = ""
            $entityName = ""
            $guid = ""

            if ($entry -match 'Exception: ([^:]+):') { $exception = $Matches[1].Trim() }
            if ($entry -match '"NodeAliasPath":\s*"([^"]+)"') { $nodeAliasPath = $Matches[1] }
            if ($entry -match '"ClassName":\s*"([^"]+)"') { $entityName = $Matches[1] }
            if ($entry -match '"NodeGUID":\s*"([0-9a-f-]+)"') { $guid = $Matches[1] }

            # Use nodeAliasPath as key if no GUID (stable across runs)
            $key = if ($guid) { $guid } elseif ($nodeAliasPath) { $nodeAliasPath } else { "$entityName-$reason-$($result.failedByGuid.Count)" }

            # Only record failure if not already succeeded in this or a later run
            if (-not $result.successByGuid.ContainsKey($key)) {
                $result.failedByGuid[$key] = @{
                    reason        = $reason
                    entity        = $entityName
                    nodeAliasPath = $nodeAliasPath
                    exception     = $exception
                }
            }
        }
    }

    return $result
}

# --- Console Log Parsing (single file) ---
function Parse-ConsoleLog {
    param([string]$Path)

    $result = @{
        commands  = @{}  # name → {started, completed, elapsed} — latest wins
        errors    = [System.Collections.Generic.List[object]]::new()
        warnings  = [System.Collections.Generic.List[object]]::new()
        media     = @{ imported = 0; missing = 0; failed = 0; missingFiles = [System.Collections.Generic.List[object]]::new() }
        forms     = [System.Collections.Generic.List[object]]::new()
        drops     = [System.Collections.Generic.List[object]]::new()
        linkedNodes = @{ deferred = 0; processed = 0; storedAsReference = 0 }
        hasReRun  = $false
    }

    if (-not $Path -or -not (Test-Path $Path)) {
        return $result
    }

    $lines = Get-Content -Path $Path -Encoding UTF8

    # Detect re-run
    if ($Path -match 'migration-run(\d+)\.log') {
        if ([int]$Matches[1] -gt 1) { $result.hasReRun = $true }
    }
    $fullText = $lines -join "`n"
    if ($fullText -match '--bypass-dependency-check') {
        $result.hasReRun = $true
    }

    # Track command starts for pairing
    $commandStarts = @{}

    # Error categorization accumulators
    $errorCategories = @{}
    $warningCategories = @{}

    foreach ($rawLine in $lines) {
        # Strip ANSI escape codes
        $line = $rawLine -replace '\x1b\[\d+m', '' -replace '\[\d+m', ''

        # Command lifecycle — Handling (outer Source.Behaviors layer only)
        if ($line -match '(\d{2}:\d{2}:\d{2}\.\d{3}).+Source\.Behaviors\.RequestHandlingBehavior\[\d+\] Handling (\w+)') {
            $commandStarts[$Matches[2]] = $Matches[1]
        }

        # Command lifecycle — Handled (outer Source.Behaviors layer only)
        if ($line -match '(\d{2}:\d{2}:\d{2}\.\d{3}).+Source\.Behaviors\.RequestHandlingBehavior\[\d+\] Handled (\w+) in elapsed: (\S+)') {
            $cmdName = $Matches[2]
            $result.commands[$cmdName] = @{
                name      = $cmdName
                started   = if ($commandStarts.ContainsKey($cmdName)) { $commandStarts[$cmdName] } else { "" }
                completed = $Matches[1]
                elapsed   = $Matches[3]
            }
        }

        # Media file imported
        if ($line -match "Media file '([0-9a-f-]+)' imported") {
            $result.media.imported++
        }

        # Media file not migrated
        if ($line -match "Media file '([0-9a-f-]+)' not migrated: (.+)") {
            $result.media.failed++
        }

        # Media file missing source
        if ($line -match 'fail:.+AssetManager\[\d+\] File (.+) does not exist') {
            $result.media.missing++
            $result.media.missingFiles.Add(@{
                path = $Matches[1].Trim()
            })
        }

        # Form copy results
        if ($line -match 'Copy of (\S+) finished.*Total=(\d+), TotalCopied=(\d+)') {
            $result.forms.Add(@{
                table      = $Matches[1]
                total      = [int]$Matches[2]
                copied     = [int]$Matches[3]
            })
        }

        # Explicit drop directive
        if ($line -match 'Content item skipped\. Reason: (.+?) NodeGUID: ([0-9a-f-]+) NodeAliasPath: (.+)') {
            $result.drops.Add(@{
                reason        = $Matches[1]
                nodeGuid      = $Matches[2]
                nodeAliasPath = $Matches[3].Trim()
            })
        }

        # Deferred linked node
        if ($line -match 'Node linked by the page \(([0-9a-f-]+)\) is not yet processed') {
            $result.linkedNodes.deferred++
        }

        # Processing deferred linked node
        if ($line -match 'Processing node ([0-9a-f-]+) linked to') {
            $result.linkedNodes.processed++
        }

        # Storing linked node as reference
        if ($line -match 'Storing the linked node as reference in ancestor') {
            $result.linkedNodes.storedAsReference++
        }

        # Categorize fail entries
        if ($line -match '^\d{2}:\d{2}:\d{2}\.\d{3} fail:') {
            $category = "Other"
            if ($line -match 'VisualBuilderPatcher') {
                $category = "VisualBuilderPatcher"
            } elseif ($line -match 'linked content item with GUID') {
                $category = "ContentItemReference"
            } elseif ($line -match 'AssetManager.+does not exist') {
                $category = "MediaFileMissing"
            } elseif ($line -match "not migrated:") {
                $category = "MediaNotMigrated"
            } elseif ($line -match 'NullReferenceException') {
                $category = "NullReference"
            } elseif ($line -match 'Cannot read keys') {
                # Terminal exception — skip
                continue
            }

            if (-not $errorCategories.ContainsKey($category)) {
                $errorCategories[$category] = @{
                    count    = 0
                    messages = [System.Collections.Generic.List[string]]::new()
                }
            }
            $errorCategories[$category].count++
            # Keep first 5 unique messages per category
            $msgSnippet = $line.Substring([Math]::Min(16, $line.Length)).Trim()
            if ($msgSnippet.Length -gt 200) { $msgSnippet = $msgSnippet.Substring(0, 200) + "..." }
            if ($errorCategories[$category].messages.Count -lt 5 -and
                -not $errorCategories[$category].messages.Contains($msgSnippet)) {
                $errorCategories[$category].messages.Add($msgSnippet)
            }
        }

        # Categorize warn entries
        if ($line -match '^\d{2}:\d{2}:\d{2}\.\d{3} warn:') {
            $category = "Other"
            if ($line -match "Value is not contained in source, field '(\w+)'") {
                $category = "MissingSourceField"
            } elseif ($line -match 'legacy format ClassFormDefinition') {
                $category = "LegacyFormDefinition"
            } elseif ($line -match "Unknown element 'schema'") {
                $category = "UnknownSchemaElement"
            }

            if (-not $warningCategories.ContainsKey($category)) {
                $warningCategories[$category] = @{
                    count    = 0
                    messages = [System.Collections.Generic.List[string]]::new()
                }
            }
            $warningCategories[$category].count++
            $msgSnippet = $line.Substring([Math]::Min(16, $line.Length)).Trim()
            if ($msgSnippet.Length -gt 200) { $msgSnippet = $msgSnippet.Substring(0, 200) + "..." }
            if ($warningCategories[$category].messages.Count -lt 5 -and
                -not $warningCategories[$category].messages.Contains($msgSnippet)) {
                $warningCategories[$category].messages.Add($msgSnippet)
            }
        }
    }

    # Convert categorized errors/warnings to list format
    foreach ($cat in $errorCategories.Keys) {
        $result.errors.Add(@{
            category = $cat
            count    = $errorCategories[$cat].count
            samples  = $errorCategories[$cat].messages
        })
    }
    foreach ($cat in $warningCategories.Keys) {
        $result.warnings.Add(@{
            category = $cat
            count    = $warningCategories[$cat].count
            samples  = $warningCategories[$cat].messages
        })
    }

    return $result
}

# --- Merge multiple protocol parse results ---
function Merge-ProtocolResults {
    param([System.Collections.Generic.List[hashtable]]$Results)

    $merged = @{
        successByGuid = @{}
        skipped = [System.Collections.Generic.List[object]]::new()
        failedByGuid  = @{}
    }

    # Process in chronological order — later runs override earlier
    foreach ($r in $Results) {
        # Successes: add/override by GUID
        foreach ($guid in $r.successByGuid.Keys) {
            $merged.successByGuid[$guid] = $r.successByGuid[$guid]
            $merged.failedByGuid.Remove($guid) | Out-Null
        }

        # Failures: add by key, but skip if already succeeded
        foreach ($key in $r.failedByGuid.Keys) {
            if (-not $merged.successByGuid.ContainsKey($key)) {
                $merged.failedByGuid[$key] = $r.failedByGuid[$key]
            }
        }

        # Skipped: accumulate (dedup by reason+entity)
        foreach ($skip in $r.skipped) {
            $existing = $merged.skipped | Where-Object { $_.reason -eq $skip.reason -and $_.entity -eq $skip.entity }
            if ($existing) {
                $existing.count = [int]$existing.count + [int]$skip.count
            } else {
                $merged.skipped.Add(@{
                    reason = $skip.reason
                    entity = $skip.entity
                    count  = $skip.count
                })
            }
        }
    }

    return $merged
}

# --- Merge multiple console parse results ---
function Merge-ConsoleResults {
    param([System.Collections.Generic.List[hashtable]]$Results)

    $merged = @{
        commands    = @{}
        errors      = [System.Collections.Generic.List[object]]::new()
        warnings    = [System.Collections.Generic.List[object]]::new()
        media       = @{ imported = 0; missing = 0; failed = 0; missingFiles = [System.Collections.Generic.List[object]]::new() }
        forms       = [System.Collections.Generic.List[object]]::new()
        drops       = [System.Collections.Generic.List[object]]::new()
        linkedNodes = @{ deferred = 0; processed = 0; storedAsReference = 0 }
        hasReRun    = $false
        runCount    = $Results.Count
    }

    # Error/warning dedup accumulators
    $errorCategories = @{}
    $warningCategories = @{}
    $dropGuids = [System.Collections.Generic.HashSet[string]]::new()
    $missingFilePaths = [System.Collections.Generic.HashSet[string]]::new()
    $formTables = [System.Collections.Generic.HashSet[string]]::new()

    foreach ($r in $Results) {
        if ($r.hasReRun) { $merged.hasReRun = $true }

        # Commands: latest execution wins per name
        foreach ($cmdName in $r.commands.Keys) {
            $merged.commands[$cmdName] = $r.commands[$cmdName]
        }

        # Media: accumulate across runs
        $merged.media.imported += $r.media.imported
        $merged.media.missing += $r.media.missing
        $merged.media.failed += $r.media.failed
        foreach ($mf in $r.media.missingFiles) {
            if ($missingFilePaths.Add($mf.path)) {
                $merged.media.missingFiles.Add($mf)
            }
        }

        # Forms: latest result per table wins
        foreach ($form in $r.forms) {
            if ($formTables.Add($form.table)) {
                $merged.forms.Add($form)
            }
        }

        # Drops: dedup by nodeGuid
        foreach ($drop in $r.drops) {
            if ($dropGuids.Add($drop.nodeGuid)) {
                $merged.drops.Add($drop)
            }
        }

        # Linked nodes: accumulate
        $merged.linkedNodes.deferred += $r.linkedNodes.deferred
        $merged.linkedNodes.processed += $r.linkedNodes.processed
        $merged.linkedNodes.storedAsReference += $r.linkedNodes.storedAsReference

        # Errors: merge categories, dedup samples
        foreach ($err in $r.errors) {
            if (-not $errorCategories.ContainsKey($err.category)) {
                $errorCategories[$err.category] = @{
                    count    = 0
                    messages = [System.Collections.Generic.List[string]]::new()
                }
            }
            $errorCategories[$err.category].count += $err.count
            foreach ($sample in $err.samples) {
                if ($errorCategories[$err.category].messages.Count -lt 5 -and
                    -not $errorCategories[$err.category].messages.Contains($sample)) {
                    $errorCategories[$err.category].messages.Add($sample)
                }
            }
        }

        # Warnings: merge categories, dedup samples
        foreach ($warn in $r.warnings) {
            if (-not $warningCategories.ContainsKey($warn.category)) {
                $warningCategories[$warn.category] = @{
                    count    = 0
                    messages = [System.Collections.Generic.List[string]]::new()
                }
            }
            $warningCategories[$warn.category].count += $warn.count
            foreach ($sample in $warn.samples) {
                if ($warningCategories[$warn.category].messages.Count -lt 5 -and
                    -not $warningCategories[$warn.category].messages.Contains($sample)) {
                    $warningCategories[$warn.category].messages.Add($sample)
                }
            }
        }
    }

    foreach ($cat in $errorCategories.Keys) {
        $merged.errors.Add(@{
            category = $cat
            count    = $errorCategories[$cat].count
            samples  = $errorCategories[$cat].messages
        })
    }
    foreach ($cat in $warningCategories.Keys) {
        $merged.warnings.Add(@{
            category = $cat
            count    = $warningCategories[$cat].count
            samples  = $warningCategories[$cat].messages
        })
    }

    return $merged
}

# --- YAML Output Generation ---
function Write-YamlOutput {
    param(
        [hashtable]$Protocol,
        [hashtable]$Console,
        [string[]]$ProtocolPaths,
        [string[]]$ConsolePaths,
        [string]$OutputPath
    )

    $sb = [System.Text.StringBuilder]::new()

    # Run metadata
    [void]$sb.AppendLine("# Migration Log Summary")
    [void]$sb.AppendLine("# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("run:")
    [void]$sb.AppendLine("  protocolLogs:")
    foreach ($p in $ProtocolPaths) {
        [void]$sb.AppendLine("    - $(Format-YamlString $p)")
    }
    [void]$sb.AppendLine("  consoleLogs:")
    foreach ($c in $ConsolePaths) {
        [void]$sb.AppendLine("    - $(Format-YamlString $c)")
    }
    [void]$sb.AppendLine("  hasReRun: $($Console.hasReRun.ToString().ToLower())")
    [void]$sb.AppendLine("  runCount: $($Console.runCount)")
    [void]$sb.AppendLine("")

    # Commands (from merged — latest per command name)
    [void]$sb.AppendLine("commands:")
    $cmdList = $Console.commands.Values | Sort-Object { $_.completed }
    if ($cmdList.Count -eq 0) {
        [void]$sb.AppendLine("  []")
    } else {
        foreach ($cmd in $cmdList) {
            [void]$sb.AppendLine("  - name: $($cmd.name)")
            [void]$sb.AppendLine("    started: $(Format-YamlString $cmd.started)")
            [void]$sb.AppendLine("    completed: $(Format-YamlString $cmd.completed)")
            [void]$sb.AppendLine("    elapsed: $(Format-YamlString $cmd.elapsed)")
        }
    }
    [void]$sb.AppendLine("")

    # Protocol summary (from merged — success by type count)
    $successByType = @{}
    foreach ($guid in $Protocol.successByGuid.Keys) {
        $type = $Protocol.successByGuid[$guid].type
        if (-not $successByType.ContainsKey($type)) { $successByType[$type] = 0 }
        $successByType[$type]++
    }

    [void]$sb.AppendLine("protocol:")
    [void]$sb.AppendLine("  success:")
    if ($successByType.Count -eq 0) {
        [void]$sb.AppendLine("    {}")
    } else {
        foreach ($key in ($successByType.Keys | Sort-Object)) {
            [void]$sb.AppendLine("    ${key}: $($successByType[$key])")
        }
    }

    [void]$sb.AppendLine("  successDetails:")
    $successList = $Protocol.successByGuid.Values | Sort-Object { $_.type }, { $_.name }
    if ($successList.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($detail in $successList) {
            [void]$sb.AppendLine("    - type: $($detail.type)")
            [void]$sb.AppendLine("      id: $($detail.id)")
            [void]$sb.AppendLine("      guid: $($detail.guid)")
            [void]$sb.AppendLine("      name: $(Format-YamlString $detail.name)")
        }
    }

    [void]$sb.AppendLine("  skipped:")
    if ($Protocol.skipped.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($skip in $Protocol.skipped) {
            [void]$sb.AppendLine("    - reason: $($skip.reason)")
            [void]$sb.AppendLine("      entity: $(Format-YamlString $skip.entity)")
            [void]$sb.AppendLine("      count: $($skip.count)")
        }
    }

    [void]$sb.AppendLine("  failed:")
    $failedList = $Protocol.failedByGuid.Values
    if ($failedList.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($fail in $failedList) {
            [void]$sb.AppendLine("    - reason: $($fail.reason)")
            [void]$sb.AppendLine("      entity: $(Format-YamlString $fail.entity)")
            [void]$sb.AppendLine("      nodeAliasPath: $(Format-YamlString $fail.nodeAliasPath)")
            [void]$sb.AppendLine("      exception: $(Format-YamlString $fail.exception)")
        }
    }
    [void]$sb.AppendLine("")

    # Console summary
    [void]$sb.AppendLine("console:")
    [void]$sb.AppendLine("  errors:")
    if ($Console.errors.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($err in $Console.errors) {
            [void]$sb.AppendLine("    - category: $($err.category)")
            [void]$sb.AppendLine("      count: $($err.count)")
            [void]$sb.AppendLine("      samples:")
            foreach ($sample in $err.samples) {
                [void]$sb.AppendLine("        - $(Format-YamlString $sample)")
            }
        }
    }

    [void]$sb.AppendLine("  warnings:")
    if ($Console.warnings.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($warn in $Console.warnings) {
            [void]$sb.AppendLine("    - category: $($warn.category)")
            [void]$sb.AppendLine("      count: $($warn.count)")
            [void]$sb.AppendLine("      samples:")
            foreach ($sample in $warn.samples) {
                [void]$sb.AppendLine("        - $(Format-YamlString $sample)")
            }
        }
    }

    [void]$sb.AppendLine("  media:")
    [void]$sb.AppendLine("    imported: $($Console.media.imported)")
    [void]$sb.AppendLine("    missing: $($Console.media.missing)")
    [void]$sb.AppendLine("    failed: $($Console.media.failed)")
    [void]$sb.AppendLine("    missingFiles:")
    if ($Console.media.missingFiles.Count -eq 0) {
        [void]$sb.AppendLine("      []")
    } else {
        foreach ($mf in $Console.media.missingFiles) {
            [void]$sb.AppendLine("      - path: $(Format-YamlString $mf.path)")
        }
    }

    [void]$sb.AppendLine("  forms:")
    if ($Console.forms.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($form in $Console.forms) {
            [void]$sb.AppendLine("    - table: $($form.table)")
            [void]$sb.AppendLine("      total: $($form.total)")
            [void]$sb.AppendLine("      copied: $($form.copied)")
        }
    }

    [void]$sb.AppendLine("  drops:")
    if ($Console.drops.Count -eq 0) {
        [void]$sb.AppendLine("    []")
    } else {
        foreach ($drop in $Console.drops) {
            [void]$sb.AppendLine("    - reason: $(Format-YamlString $drop.reason)")
            [void]$sb.AppendLine("      nodeGuid: $($drop.nodeGuid)")
            [void]$sb.AppendLine("      nodeAliasPath: $(Format-YamlString $drop.nodeAliasPath)")
        }
    }

    [void]$sb.AppendLine("  linkedNodes:")
    [void]$sb.AppendLine("    deferred: $($Console.linkedNodes.deferred)")
    [void]$sb.AppendLine("    processed: $($Console.linkedNodes.processed)")
    [void]$sb.AppendLine("    storedAsReference: $($Console.linkedNodes.storedAsReference)")

    # Write output
    $outputDir = Split-Path $OutputPath -Parent
    if (-not (Test-Path $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }
    Set-Content -Path $OutputPath -Value $sb.ToString() -Encoding UTF8 -NoNewline
    Write-Host "Log summary written to: $OutputPath"
}

# --- Main ---

if (-not $LogDir) {
    Write-Error "LogDir parameter is required. Pass the directory containing protocol*.txt and migration-run*.log files."
    exit 1
}

if (-not (Test-Path $LogDir)) {
    Write-Error "Log directory does not exist: $LogDir"
    exit 1
}

if (-not $OutputPath) {
    $OutputPath = Join-Path $LogDir "parsed-log-summary.yaml"
}

# Discover all log files
$protocolFiles = @(Find-AllProtocolLogs -Dir $LogDir)
$consoleFiles = @(Find-AllConsoleLogs -Dir $LogDir)

if ($protocolFiles.Count -eq 0 -and $consoleFiles.Count -eq 0) {
    Write-Error "No log files found in $LogDir. Expected protocol*.txt and/or migration-run*.log files."
    exit 1
}

Write-Host "Parsing migration logs from: $LogDir"
Write-Host "  Protocol logs: $($protocolFiles.Count) file(s)"
foreach ($f in $protocolFiles) { Write-Host "    $($f.Name)" }
Write-Host "  Console logs:  $($consoleFiles.Count) file(s)"
foreach ($f in $consoleFiles) { Write-Host "    $($f.Name)" }

# Parse all files
$protocolResults = [System.Collections.Generic.List[hashtable]]::new()
foreach ($f in $protocolFiles) {
    $protocolResults.Add((Parse-ProtocolLog -Path $f.FullName))
}

$consoleResults = [System.Collections.Generic.List[hashtable]]::new()
foreach ($f in $consoleFiles) {
    $consoleResults.Add((Parse-ConsoleLog -Path $f.FullName))
}

# Merge across runs
$protocol = if ($protocolResults.Count -gt 0) { Merge-ProtocolResults -Results $protocolResults } else {
    @{ successByGuid = @{}; skipped = [System.Collections.Generic.List[object]]::new(); failedByGuid = @{} }
}
$console = if ($consoleResults.Count -gt 0) { Merge-ConsoleResults -Results $consoleResults } else {
    @{ commands = @{}; errors = [System.Collections.Generic.List[object]]::new(); warnings = [System.Collections.Generic.List[object]]::new();
       media = @{ imported = 0; missing = 0; failed = 0; missingFiles = [System.Collections.Generic.List[object]]::new() };
       forms = [System.Collections.Generic.List[object]]::new(); drops = [System.Collections.Generic.List[object]]::new();
       linkedNodes = @{ deferred = 0; processed = 0; storedAsReference = 0 }; hasReRun = $false; runCount = 0 }
}

# Make paths relative for YAML output
$protocolRelPaths = $protocolFiles | ForEach-Object {
    [System.IO.Path]::GetRelativePath($WorkspaceRoot, $_.FullName) -replace '\\', '/'
}
$consoleRelPaths = $consoleFiles | ForEach-Object {
    [System.IO.Path]::GetRelativePath($WorkspaceRoot, $_.FullName) -replace '\\', '/'
}
if (-not $protocolRelPaths) { $protocolRelPaths = @() }
if (-not $consoleRelPaths) { $consoleRelPaths = @() }

# Write
Write-YamlOutput -Protocol $protocol -Console $console `
    -ProtocolPaths $protocolRelPaths -ConsolePaths $consoleRelPaths `
    -OutputPath $OutputPath

# Summary
$totalSuccess = $protocol.successByGuid.Count
$totalSkipped = ($protocol.skipped | Measure-Object -Property count -Sum).Sum
$totalFailed = $protocol.failedByGuid.Count
$totalErrors = ($console.errors | Measure-Object -Property count -Sum).Sum
$totalWarnings = ($console.warnings | Measure-Object -Property count -Sum).Sum

Write-Host ""
Write-Host "Summary (merged across $($protocolFiles.Count + $consoleFiles.Count) log files):"
Write-Host "  Protocol: $totalSuccess success, $totalSkipped skipped, $totalFailed failed"
Write-Host "  Console:  $totalErrors errors, $totalWarnings warnings"
Write-Host "  Media:    $($console.media.imported) imported, $($console.media.missing) missing, $($console.media.failed) failed"
Write-Host "  Commands: $($console.commands.Count) completed"

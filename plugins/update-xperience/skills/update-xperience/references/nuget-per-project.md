# NuGet Package Update — Per-Project Package References

Use this when no `Directory.Packages.props` file governs the target solution/project (detected in precondition 6), meaning packages are managed directly in `.csproj` or `Directory.Build.props` files.

1. Search all `.csproj` and `Directory.Build.props` files for `<PackageReference Include="Kentico.Xperience.*" Version="*" />` entries (any version).
2. For each `Kentico.Xperience.*` package:
   - **If the version matches the current version identified in Step 1**: update it to the target version.
   - **If the version differs from the current version**: leave it untouched and note it in the final summary as an "intentionally pinned" package.
3. If any `Kentico.Xperience.*` package uses a `-preview` or `-prerelease` suffix and its base version matches the current version, update it to the corresponding pre-release of the new version if one is available.
4. Do not update packages outside the `Kentico.Xperience.*` namespace, even if they appear outdated.

# NuGet Package Update — Central Package Management

Use this when a `Directory.Packages.props` file governs the target solution/project (detected in precondition 6).

1. Open the applicable `Directory.Packages.props` file selected in precondition 6 (do not assume repository root).
2. Identify all `<PackageVersion Include="Kentico.Xperience.*" Version="*" />` entries (any version).
3. For each `Kentico.Xperience.*` package:
   - **If the version matches the current version identified in Step 1**: replace it with the target version.
   - **If the version differs from the current version** (e.g., intentionally pinned to an older or newer value, or at a different patch level): leave it untouched and note it in the final summary as an "intentionally pinned" package.
4. If any `Kentico.Xperience.*` package uses a `-preview` or `-prerelease` suffix and its base version matches the current version, update it to the corresponding pre-release of the new version if one is available.
5. Do not update packages outside the `Kentico.Xperience.*` namespace, even if they appear outdated.

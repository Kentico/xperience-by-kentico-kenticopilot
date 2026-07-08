# CD deployment summary

## Deployment scope

- **Change selectors:** {PR numbers or commit range, as provided by the user}
- **CI Repository:** {path to the CI Repository}
- **Target config:** {path to repository.config}

## Analyzed changes

<!-- Commit mode: list every commit in the range in both tables below.
     PR mode: list the analyzed PRs instead (use a "PR" column in place of "Commit"). -->

### Included in deployment scope (business/feature)

| Commit | Subject | Note |
| ------ | ------- | ---- |
| {short hash} | {subject} | {optional note} |

### Excluded as Xperience update-only

<!-- When nothing was excluded, replace the table with: "No commits were classified as Xperience update-only." -->

| Commit | Subject | Reason for exclusion |
| ------ | ------- | -------------------- |
| {short hash} | {subject} | {classification signal, e.g. "hotfix version bump in title, bulk CI churn"} |

## Deployment configuration

- **RestoreMode:** {Create | CreateUpdate} — {reasoning based on git history of the scoped files}
- **Included object types:** {list of IncludedObjectTypes entries}
- **Object filters (code names by object type):**
  - `{object type}`: {code names}
- **Included content item types:** {IncludedContentItemsOfType entries, or "none — no content items in deployment scope"}
- **Content item filters:** {IncludedContentItemNames entries, or "none"}

## repository.config changes

{Concise diff or description of exactly what changed in the config, including everything that was removed
during regeneration. If entries not produced by this skill were found (manual exclusions etc.), state
whether they were kept or removed and who decided.}

## Verification

- **Store / package export:** {run (command or script used) | not run — reason}
- **Verification script:** {not run — reason | X passed, Y warning(s), Z failure(s)}

<!-- When the script reported warnings or failures, paste the [WARN]/[FAIL] lines verbatim below,
     each followed by how it was resolved or why it is acceptable. -->

## Follow-ups

{Anything the user should still review, decide, or run — or "None."}

# CD deployment summary

## Deployment scope

- **Change selector:** {PR numbers or commit range, as provided by the user}
- **CI Repository:** {path to the CI Repository}
- **Target config:** {path to repository.config}

## Analyzed changes

### Included in deployment scope (business/feature)

| {Commit or PR} | Subject | Note |
| --------------- | ------- | ---- |
| {short hash or PR number} | {subject} | {optional note} |

### Excluded as Xperience update-only

| {Commit or PR} | Subject | Reason for exclusion |
| --------------- | ------- | --------------------- |
| {short hash or PR number} | {subject} | {classification signal, e.g. "hotfix version bump in title, bulk CI churn"} |

{If nothing was excluded, replace the table above with: "No commits were classified as Xperience update-only."}

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
- **Verification script:** {not run — reason | X passed, Y warning(s), Z failure(s); paste [WARN]/[FAIL] lines verbatim, each followed by how it was resolved or why it is acceptable}

## Follow-ups

{Anything the user should still review, decide, or run — or "None."}

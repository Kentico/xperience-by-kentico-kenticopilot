# KX13 Content Auditor

A CLI tool that reads a Kentico Xperience 13 (KX13) database and exports the content model as structured JSON files. Useful for auditing site content, planning migrations, and analyzing page types, content trees, forms, custom tables, and page builder usage.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later
- SQL Server with a KX13 database (SQL Server 2019+ or LocalDB)

## Setup

1. Clone the repository
2. Configure the connection string in `kx13-content-audit/src/KX13.ContentAuditor.CLI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "ConnectionString": "Data Source=YOUR_SERVER;Initial Catalog=YOUR_KX13_DB;Integrated Security=True;Encrypt=False;"
  }
}
```

Alternatively, create an `appsettings.development.json` file in the same directory (this file is git-ignored). The CLI loads it automatically as a local override when present.

1. Build the solution:

```bash
dotnet build kx13-content-audit/src/KX13.ContentAuditor.slnx
```

## Usage

Run from the workspace root:

```bash
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI
```

### Full export (no arguments)

Exports the entire content model as individual JSON files:

```bash
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI
```

This produces:

- `sites.json` — Sites with cultures, content tree, page builder configs, and custom field values
- `page-types.json` — All page types with field definitions
- `custom-tables.json` — Custom tables with fields and alternative forms
- `custom-modules.json` — Custom modules with classes and references
- `forms.json` — BizForms with fields, validation rules, and alternative forms
- `page-builder-components.json` — Discovered widgets, sections, and page templates in use
- `content-reference-graph.json` — Cross-content reference map (page selectors, media selectors, etc.)
- `relationships.json` — Page-to-page relationships, including ad-hoc Pages fields and named relationships

### Selective export

Export only specific parts of the content model by passing flags:

```bash
# Export just page types
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI -- --page-types

# Export sites and forms
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI -- --sites --forms

# Export only page relationships
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI -- --relationships

# Export page builder components to a custom directory
dotnet run --project kx13-content-audit/src/KX13.ContentAuditor.CLI -- --page-builder-components --output ./my-output
```

### Available flags

#### Export options

| Flag | Description |
|---|---|
| `--sites` | Sites with cultures, content tree, and assigned types |
| `--page-types` | All page types with field definitions |
| `--page-builder-components` | Discovered page builder components (widgets, sections, templates) |
| `--custom-modules` | Custom modules with their classes |
| `--custom-tables` | Custom tables with fields |
| `--forms` | BizForms with fields and alternative forms |
| `--relationships` | Page-to-page relationships and Pages-field reuse links |
| `--report` | Generate a Markdown content model report (`content-model-report.md`) |

Running without export options exports the full content model (all JSON files + report).

#### Filter options

| Flag | Description |
|---|---|
| `--site-name <name>` | Filter by site code name (exact match, e.g. `DancingGoatMvc`) |
| `--class-name <pattern>` | Filter by class name pattern (`*` wildcard, comma-separated, e.g. `"DancingGoat.*,CMS.MenuItem"`) |
| `--page-path <prefix>` | Filter content tree by node alias path prefix (e.g. `/Articles`) |

#### Other options

| Flag | Description |
|---|---|
| `--output <path>` | Output directory (default: `audit-results/` under the project root) |
| `--help`, `-h` | Show help |

## Output

JSON files are written to `audit-results/` under the auditor project root by default. This directory is git-ignored.

## Project Structure

```
src/
├── KX13.ContentAuditor.CLI/            # Console entry point, argument parsing, JSON export
├── KX13.ContentAuditor.Application/    # Orchestration service (ContentModelService)
└── KX13.ContentAuditor.DataAccess/     # Database access layer
    ├── Models/                         # 25 POCO model classes
    ├── Repositories/                   # SQL queries + result mapping
    │   └── Interfaces/                 # Repository contracts
    ├── Parsing/                        # XML/JSON parsers (ClassFormDefinition, PageBuilder)
    ├── Analysis/                       # Component discovery + content reference analysis
    └── DbReader/                       # Raw ADO.NET query executor
```

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

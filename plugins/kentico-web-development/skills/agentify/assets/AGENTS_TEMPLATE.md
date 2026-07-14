# AGENTS.md

## Project overview

- **Name:** {project name}
- **Stack:** {list of important technologies used in the project}
- **Purpose:** {one sentence on what this site/app does}
- **Running app URL:** {the URL where the running app can be accessed}

## Repository layout

| Path             | What lives there                  |
| ---------------- | --------------------------------- |
| {important path} | {description of what lives there} |
| {...}            | {...}                             |

## Useful commands

| Task      | Command        |
| --------- | -------------- |
| Run site  | `dotnet run`   |
| Build     | `dotnet build` |
| Run tests | `dotnet test`  |
| {...}     | {...}          |

## Content changes

If you change the site's content model (add or remove fields, define new content types or schemas, etc.), you must run the following commands to regenerate the code files.

{List of relevant commands to run code generation, see: https://docs.kentico.com/documentation/developers-and-admins/api/generate-code-files-for-system-objects}

## Coding conventions

{Bullet points on coding conventions used in the project, e.g. naming conventions, formatting rules, etc.}

## Kentico MCP Servers

- **Kentico Docs MCP** is the **primary source** for any question about Xperience by Kentico projects. Prefer it over web search and over your prior knowledge.
- **Kentico Management MCP** is used to work with content inside the Xperience by Kentico projects. Prefer it over manual management using the website admin interface.

## Validation of changes

- Always build project and run relevant tests after making changes.
- Always validate user-facing changes in the browser for content, layout, styling, and localization correctness before committing.

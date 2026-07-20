# MCP server setup

MCP requirements vary by migration stage. Add the servers needed for your migration using the configuration below.

## Servers

- **Kentico Docs MCP server**
  - `https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server`
  - Used to search and fetch the official Xperience by Kentico documentation.
  - Required by the codebase-migration skills except `migrate-code-page-visual`.

- **Context7 MCP server**
  - `https://context7.com`
  - Optional lookup for KX13 source API references. The KX13 documentation is indexed in the [`websites/kentico_13`](https://context7.com/websites/kentico_13) library.

- **Playwright MCP server**
  - `https://github.com/microsoft/playwright-mcp`
  - Used to compare migrated pages against the original KX13 site and fix visual discrepancies.
  - Required by `migrate-code-component`, `migrate-code-page`, and `migrate-code-page-visual`.

## How to add the servers

Add the server to the `.mcp.json` file at your workspace root (create the file if it doesn't exist):

```json
{
  "mcpServers": {
    "kentico.docs.mcp": {
      "type": "http",
      "url": "https://docs.kentico.com/mcp"
    },
    "context7": {
      "type": "http",
      "url": "https://mcp.context7.com/mcp"
    },
    "playwright-mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["@playwright/mcp@latest", "--viewport-size=1920x1080"]
    }
  }
}
```

## Other AI assistants

The definitions above are standard HTTP/stdio MCP servers — consult your assistant's MCP documentation for where to place the equivalent configuration.

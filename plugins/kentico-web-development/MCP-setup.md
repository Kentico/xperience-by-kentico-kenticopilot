# MCP server setup

MCP requirements vary by skill. Add the servers needed for your tasks using the configuration below.

## Servers

- **Kentico Docs MCP server**
  - `https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server`
  - Used to search and fetch the official Xperience by Kentico documentation.
  - Required by `design-to-content`, `page-builder-widgets`, `page-builder-structure`, and `content-retrieval`.

- **Kentico Management MCP server**
  - `https://docs.kentico.com/documentation/developers-and-admins/api/management-api/configure-management-mcp-server`
  - Used to work with content in a running application (requires per-project setup).
  - Checked by `agentify` and recommended for `design-validation`.

## How to add the servers

Add the server to the `.mcp.json` file at your workspace root (create the file if it doesn't exist):

```json
{
  "mcpServers": {
    "kentico.docs.mcp": {
      "type": "http",
      "url": "https://docs.kentico.com/mcp"
    },
    "xperience-management-mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["@kentico/management-api-mcp@latest"],
      "env": {
        "MANAGEMENT_API_URL": "http://localhost:<YourAppPort>/kentico-api/management",
        "MANAGEMENT_API_SECRET": "<YourSecretValue>"
      }
    }
  }
}
```

## Other AI assistants

The definitions above are standard HTTP/stdio MCP servers — consult your assistant's MCP documentation for where to place the equivalent configuration.

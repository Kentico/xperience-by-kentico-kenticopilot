# MCP server setup

This plugin uses the MCP servers listed below. Add them to your AI coding assistant.

| Server | Provides | Status |
|---|---|---|
| [Kentico Docs MCP server](https://docs.kentico.com/x/mcp_server_xp) | Search and retrieval over the official Xperience by Kentico documentation | Required for code migration |
| [Playwright MCP server](https://github.com/microsoft/playwright-mcp) | Browser automation for comparing rendered pages | Required for visual comparison |
| [Context7 MCP server](https://context7.com) | Lookup over indexed third-party library documentation, including Kentico Xperience 13 | Optional |

Documentation lookups confirm the target APIs. Browser automation compares a migrated page with the original KX13 page. Context7 covers KX13-era APIs. The KX13 documentation is indexed as the [`websites/kentico_13`](https://context7.com/websites/kentico_13) library.

In assistants that read a workspace configuration file, the definitions go in `.mcp.json` at your workspace root. Create the file if it doesn't exist:

```json
{
  "mcpServers": {
    "kentico.docs.mcp": {
      "type": "http",
      "url": "https://docs.kentico.com/mcp"
    },
    "playwright-mcp": {
      "type": "stdio",
      "command": "npx",
      "args": ["@playwright/mcp@latest", "--viewport-size=1920x1080"]
    },
    "context7": {
      "type": "http",
      "url": "https://mcp.context7.com/mcp"
    }
  }
}
```

Plugin installation does not create this configuration. See [Check the usage requirements](../../docs/Usage-Guide.md#check-the-usage-requirements) in the usage guide.

These are standard HTTP and stdio MCP servers. For the equivalent configuration and its location, see your assistant's MCP documentation.

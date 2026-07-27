# MCP server setup

This plugin uses the MCP servers listed below. Add them to your AI coding assistant.

| Server | Provides | Status |
|---|---|---|
| [Kentico Docs MCP server](https://docs.kentico.com/x/mcp_server_xp) | Search and retrieval over the official Xperience by Kentico documentation | Required |

Documentation lookups keep generated code aligned with the current APIs.

In assistants that read a workspace configuration file, the definitions go in `.mcp.json` at your workspace root. Create the file if it doesn't exist:

```json
{
  "mcpServers": {
    "kentico.docs.mcp": {
      "type": "http",
      "url": "https://docs.kentico.com/mcp"
    }
  }
}
```

Plugin installation does not create this configuration. See [Check the usage requirements](../../docs/Usage-Guide.md#check-the-usage-requirements) in the usage guide.

These are standard HTTP MCP servers. For the equivalent configuration and its location, see your assistant's MCP documentation.

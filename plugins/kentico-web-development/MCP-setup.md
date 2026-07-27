# MCP server setup

This plugin uses the MCP servers listed below. Add them to your AI coding assistant.

| Server | Provides | Status |
|---|---|---|
| [Kentico Docs MCP server](https://docs.kentico.com/x/mcp_server_xp) | Search and retrieval over the official Xperience by Kentico documentation | Required |
| [Kentico Management MCP server](https://docs.kentico.com/x/configure_management_mcp_xp) | Read and write access to content in a running application | Required |

Documentation lookups keep content models and generated components aligned with the current APIs. Management MCP enables object creation and manipulation.

The Management server also requires changes to the application itself. Follow the steps in the [product documentation](https://docs.kentico.com/x/configure_management_mcp_xp).

In assistants that read a workspace configuration file, the definitions go in `.mcp.json` at your workspace root. Create the file if it doesn't exist:

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

Plugin installation does not create this configuration. See [Check the usage requirements](../../docs/Usage-Guide.md#check-the-usage-requirements) in the usage guide.

These are standard HTTP and stdio MCP servers. For the equivalent configuration and its location, see your assistant's MCP documentation.

# MCP server setup

The skills in this plugin work best with the following MCP servers. The plugin does not register them automatically — add them to your workspace using the copy-paste snippets below.

## Recommended servers

- **Kentico Docs MCP server**
  - `https://docs.kentico.com/documentation/developers-and-admins/installation/mcp-server`
  - Used to search and fetch the official Xperience by Kentico documentation.

## How to add the servers

Add the server to the `.mcp.json` file at your workspace root (create the file if it doesn't exist):

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

## Other AI assistants

The definitions above are standard HTTP/stdio MCP servers — consult your assistant's MCP documentation for where to place the equivalent configuration.

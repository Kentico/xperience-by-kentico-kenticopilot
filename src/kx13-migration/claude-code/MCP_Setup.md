# MCP Setup

## Add Kentico Docs MCP server

Run the following commands in the terminal to add MCP servers to your Claude MCP configuration:

```bash
claude mcp add-json kentico.docs.mcp '{"type": "http", "url": "https://docs.kentico.com/mcp"}'
claude mcp add-json playwright-mcp '{"command": "npx", "args": ["@playwright/mcp@latest", "--viewport-size=1920x1080"]}'
```

# Terraform MCP Server Setup (Reference)

This file documents how to configure the Terraform MCP server for Cursor, based on [awesome-copilot](https://github.com/github/awesome-copilot) `plugins/partners/agents/terraform.md`.

## Prerequisites

- Docker installed and running
- (Optional) HCP Terraform / Terraform Cloud token for workspace management

## Cursor MCP Configuration

Add the Terraform MCP server to your Cursor MCP config. The config file is typically at:
- **macOS**: `~/.cursor/mcp.json` or via Cursor Settings → MCP

### Example MCP Config

```json
{
  "mcpServers": {
    "terraform": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "-e", "TFE_TOKEN=${TFE_TOKEN}",
        "-e", "TFE_ADDRESS=${TFE_ADDRESS}",
        "-e", "ENABLE_TF_OPERATIONS=${ENABLE_TF_OPERATIONS}",
        "hashicorp/terraform-mcp-server:latest"
      ]
    }
  }
}
```

### Environment Variables (Optional)

| Variable | Description |
|----------|-------------|
| `TFE_TOKEN` | HCP Terraform / Terraform Cloud API token for workspace operations |
| `TFE_ADDRESS` | HCP Terraform address (e.g. `app.terraform.io`) |
| `ENABLE_TF_OPERATIONS` | Set to `true` to enable plan/apply operations |

## Capabilities

- **Registry**: Search providers, modules, and policies
- **Code generation**: Get latest versions and documentation
- **HCP Terraform** (with token): Workspace management, runs, variable sets

## Source

Adapted from: `awesome-copilot/plugins/partners/agents/terraform.md`

# Cursor Config Reference

Files in this directory are reference copies from [awesome-copilot](https://github.com/github/awesome-copilot) for Terraform support.

## Contents

| File | Source | Purpose |
|------|--------|---------|
| `terraform-mcp-setup.md` | `plugins/partners/agents/terraform.md` | MCP server setup for Terraform registry & HCP Terraform |
| `terraform-agent-instructions.md` | `plugins/partners/agents/terraform.md` | Full agent instructions (registry, best practices, workflows) |
| `terraform-iac-reviewer.md` | `agents/terraform-iac-reviewer.agent.md` | IaC review checklist and safety practices |

## Rules

Terraform conventions are applied via `.cursor/rules/terraform-conventions.mdc` when working with `*.tf`, `*.tfvars`, or `*.tflint.hcl` files.

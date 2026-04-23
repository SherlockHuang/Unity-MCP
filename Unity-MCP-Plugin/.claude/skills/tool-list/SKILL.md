---
name: tool-list
description: List available MCP tools for lightweight discovery. Optionally filter by regex across tool names and argument names.
---

# Tool / List

Lightweight discovery only: this tool returns tool names and, when requested, input names. It does not return tool descriptions, input descriptions, or schemas. Use `tool-get-detail` with `detailLevel: summary` for compact usage details, and `detailLevel: full` for complete schemas.

## How to Call

```bash
unity-mcp-cli run-tool tool-list --input '{
  "regexSearch": "string_value",
  "includeInputs": "string_value"
}'
```

> For complex input (multi-line strings, code), save the JSON to a file and use:
> ```bash
> unity-mcp-cli run-tool tool-list --input-file args.json
> ```
>
> Or pipe via stdin (recommended):
> ```bash
> unity-mcp-cli run-tool tool-list --input-file - <<'EOF'
> {"param": "value"}
> EOF
> ```


### Troubleshooting

If `unity-mcp-cli` is not found, either install it globally (`npm install -g unity-mcp-cli`) or use `npx unity-mcp-cli` instead.
Read the /unity-initial-setup skill for detailed installation instructions.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `regexSearch` | `string` | No | Regex pattern to filter tools. Matches against tool name and argument names. |
| `includeInputs` | `any` | No | Include input argument names in the result. Default: None |

### Input JSON Schema

```json
{
  "type": "object",
  "properties": {
    "regexSearch": {
      "type": "string"
    },
    "includeInputs": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+InputRequest"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+InputRequest": {
      "type": "string",
      "enum": [
        "None",
        "Inputs"
      ]
    }
  }
}
```

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInfoData[]"
    }
  },
  "$defs": {
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInfoData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Tool name."
        },
        "inputs": {
          "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInputData[]",
          "description": "Tool input arguments."
        }
      },
      "description": "MCP tool information."
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInputData[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInputData",
        "description": "MCP tool input argument."
      }
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInputData": {
      "type": "object",
      "properties": {
        "name": {
          "type": "string",
          "description": "Argument name."
        }
      },
      "description": "MCP tool input argument."
    },
    "com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInfoData[]": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Editor.API.Tool_Tool+ToolInfoData",
        "description": "MCP tool information."
      }
    }
  },
  "required": [
    "result"
  ]
}
```


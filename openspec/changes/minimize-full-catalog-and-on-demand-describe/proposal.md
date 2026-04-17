## Why

Unity-MCP's full tool catalog still injects more input-schema detail than Codex app/CLI can efficiently carry. We need a lighter default discovery surface so standard MCP tool discovery remains usable while detailed tool contracts are fetched only when a model actually needs them.

## What Changes

- Minimize the default full-catalog input schema exposed by Unity-MCP so tool discovery keeps only the fields needed for tool selection and basic invocation planning.
- Preserve an explicit on-demand detail path through `tool-get-detail` for advanced argument structure, full schema retrieval, and higher-fidelity tool guidance.
- Update documentation and tests to describe and validate the minimized full-catalog behavior and the on-demand detail path.

## Capabilities

### New Capabilities
- `compact-tool-catalog`: Expose a minimized default MCP tool catalog that keeps discovery-compatible input schemas small while preserving a separate on-demand detail path for richer tool contracts.

### Modified Capabilities
- None.

## Impact

- Affected code:
  - `Unity-MCP-Plugin/Assets/root/Runtime` and editor tool metadata surfaces involved in exposing tool schemas
  - `Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Tool.GetDetail.cs`
  - `Unity-MCP-Server/` standard MCP tool-list exposure path
- Affected behavior:
  - Standard MCP `tools/list` consumers receive a lighter default schema shape
  - Models must rely more intentionally on on-demand detail fetches for advanced tool usage
- Affected docs/tests:
  - Root and mirrored READMEs
  - Server/plugin tests
  - Unity editor tests for tool detail metadata

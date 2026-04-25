## Why

Unity-MCP's discovery/catalog payload has already been reduced, but frequent runtime tool results can still consume excessive context during normal Unity automation. The heaviest current pattern is test execution output: repeated fully qualified test names duplicate assembly/namespace/class prefixes across many results, while project workflows also rely on prefab/widget reports and GameObject inspection where summary-first output would usually be enough.

## What Changes

- Add a compact execution-result contract for `tests-run`, the highest-impact high-volume Unity tool response in this repository.
- Introduce an explicit result format for test execution so callers can choose the current flat response or a grouped compact response that hoists repeated assembly/namespace/class prefixes.
- Extend the typed `TestRunResponse` model with optional grouped result fields so output schemas remain accurate and structured.
- Preserve failure diagnostics by default while avoiding repeated passing-test details and duplicated path/name prefixes unless explicitly requested.

No existing output mode is removed in this change.

## Capabilities

### New Capabilities
- `compact-tool-results`: Execution-result payload contracts for high-volume Unity-MCP tools, focused in this change on compact typed `tests-run` results.

### Modified Capabilities
- None.

## Impact

- Affected code:
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests.Run.cs`
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests/TestResultCollector.cs`
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests/*.cs`
  - tests covering Unity test-run response serialization and compatibility
- Affected downstream/project wrapper behavior:
  - Project wrappers such as `tests-run-safe` can forward the new test result format without owning a separate conversion layer.
- Affected API behavior:
  - `tests-run` gains an opt-in compact/tree-style result shape while retaining the existing flat result shape for compatibility.
  - `TestRunResponse` remains the advertised typed response schema; compact mode populates typed grouped fields rather than returning ad-hoc JSON.
  - compact output keeps failure/skipped diagnostics useful and keeps logs opt-in.
- Affected docs/specs:
  - OpenSpec defines the contract before implementation.
  - Documentation should teach summary-first runtime result usage separately from catalog discovery/detail usage.

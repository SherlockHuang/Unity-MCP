## Why

The compact `tests-run` tree format was added to reduce repeated Unity test output, but review found two payload details that still work against that goal: each method leaf repeats the full test name, and tree responses still carry an empty legacy `Results` list. This change tightens the compact-output contract before shipping the fix.

## What Changes

- Make compact tree leaves omit repeated full test identities by default.
- Preserve the original full test name only as a fallback when parsing cannot provide enough structured identity.
- Omit the legacy flat `Results` list from tree responses instead of serializing an empty collection.
- Add focused tests for both token-reduction behaviors while keeping flat mode compatible.

## Capabilities

### New Capabilities
- `compact-tool-results`: Refinements to compact `tests-run` result payloads that remove avoidable repeated fields while preserving structured diagnostics.

### Modified Capabilities
- None.

## Impact

- Affected code:
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests/TestResultResponseBuilder.cs`
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests/TestResultGroupData.cs`
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/Scripts/API/Tool/Tests/TestRunResponse.cs`
  - `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Tests/Editor/Tool/Tests/TestsRunResponseBuilderTests.cs`
- Affected behavior:
  - `tests-run resultFormat=Tree` remains structured but avoids repeated full identities and empty flat result payloads.
  - `tests-run resultFormat=Flat` remains the compatibility default.

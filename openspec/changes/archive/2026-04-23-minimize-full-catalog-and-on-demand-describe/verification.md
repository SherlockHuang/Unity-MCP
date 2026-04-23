# Verification

## Server-Side

- Command: `dotnet test G:/github/Unity-MCP/Unity-MCP-Server/Tests/com.IvanMurzak.Unity.MCP.Server.Tests.csproj --filter FullyQualifiedName~ToolCatalogSchemaMinifierTests`
- Outcome: passed
- Result: 4 tests passed, 0 failed, 0 skipped

## Unity-Side

- Correct project context: `G:/github/Unity-MCP/Unity-Tests/2022.3.62f3`
- Why this context: the versioned `Unity-Tests/*` projects are the actual Unity consumer projects in this repo. Their `Packages/manifest.json` files reference `com.ivanmurzak.unity.mcp` via `file:./../../../Unity-MCP-Plugin/Assets/root` and mark the package as `testables`, which exposes the package Editor test assembly `com.IvanMurzak.Unity.MCP.Editor.Tests`.

- Historical note: the earlier `tests_run_safe` attempts against the currently connected editor were inconclusive because that editor session was not running the `Unity-Tests` project context that contains the package test assembly.

- Evidence: `evidence/ToolGetDetailTests.xml`
- Outcome: passed
- Result: 6 tests passed, 0 failed, 0 skipped
- Note: a narrowly scoped Unity-test fix was required so `CreateInternalErrorResult_SanitizesClientFacingMessage` explicitly expects the intentional error log emitted by `Tool_Tool.CreateInternalErrorResult(...)`. Without that expectation, Unity Test Runner treated the log as an unhandled failure even though the behavior assertions were correct.

- Evidence: `evidence/ToolCatalogMinimizationTests.xml`
- Outcome: passed
- Result: 2 tests passed, 0 failed, 0 skipped

## Evidence

The raw Unity test results are copied into this change directory so the verification claim is auditable without relying on ignored workspace output:

- `evidence/ToolGetDetailTests.xml`
- `evidence/ToolCatalogMinimizationTests.xml`

Those snapshots show:

- `ToolGetDetailTests`: 6/6 passed
- `ToolCatalogMinimizationTests`: 2/2 passed
- Both runs executed in `G:/github/Unity-MCP/Unity-Tests/2022.3.62f3` and completed successfully in EditMode

## Readiness

This change is ready for `/opsx:apply`.

- Server-side verification passed.
- Unity-side targeted batch-mode verification passed in the correct repo test context, and the supporting NUnit XML now lives under this change directory.

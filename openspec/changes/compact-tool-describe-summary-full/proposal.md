## Why

After minimizing the default full catalog, the next context hotspot is the on-demand detail path itself: `tool-get-detail` still returns more payload than most model turns need. We need a deliberate `summary` versus `full` detail contract, selected by an explicit request parameter, so models can usually stay on a compact path and only fetch the heaviest schema/detail payload when truly necessary.

## What Changes

- Introduce an explicit `detailLevel: summary|full` request contract for `tool-get-detail`, with `summary` as the default and `full` as the opt-in heavy path, while clearly defining how the existing `includeSchemas` and `includeParsedArguments` flags behave for compatibility.
- Define exactly which request and response fields belong to the compact summary, full detail, and failure paths so the contract is predictable and testable.
- Ensure the default on-demand detail response is optimized for model comprehension and invocation planning rather than exhaustive schema transfer.
- Update tests and documentation to reflect the new summary/full split, including preservation of the existing structured failure contract.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `compact-tool-catalog`: Refine the post-discovery workflow so the minimized full catalog naturally hands off to compact summary detail before full schema retrieval, with an explicit `detailLevel: summary|full` contract on `tool-get-detail` and a stricter summary/full response boundary.

## Impact

- Affected code:
  - `Unity-MCP-Plugin/Assets/root/Editor/Scripts/API/Tool/Tool.GetDetail.cs`
  - supporting tests under `Unity-MCP-Plugin/Assets/root/Tests/Editor/Tool/Tool/`
  - any documentation that describes the recommended discovery/detail workflow
- Affected behavior:
  - default tool detail becomes smaller and more intentionally shaped via `detailLevel: summary`
  - callers explicitly opt into the heaviest detail payload via `detailLevel: full`
  - the existing structured failure path remains intact
- Affected docs/tests:
  - root and mirrored README files
  - OpenSpec artifacts for the new compact describe contract
  - Unity-side tests for summary versus full detail behavior

## Dependency Note

This change is a follow-on refinement of `minimize-full-catalog-and-on-demand-describe`. It assumes the earlier change's `compact-tool-catalog` contract exists and tightens the detail step that follows minimized full-catalog discovery.

## Context

The previous change reduced the default full catalog payload for standard MCP discovery and positioned `tool-get-detail` as the richer on-demand metadata path. That solved the worst startup schema inflation, but the next context hotspot is the detail response itself: even single-tool detail can still be heavier than most turns need. We now want a disciplined “summary first, full on demand” contract so the typical follow-up after discovery stays small and predictable.

This change is centered on the Unity-side detail surface and the documentation that teaches callers how to use it. It should not reintroduce heavy payloads into the full catalog, and it should not require new client capabilities beyond an explicit `detailLevel: summary|full` request parameter on the existing detail endpoint.

## Goals / Non-Goals

**Goals:**
- Define a compact default detail response for single-tool inspection.
- Preserve an explicit opt-in full-detail mode for richer schema transfer.
- Keep the summary and full responses structurally predictable.
- Update docs so the recommended path becomes: minimized full catalog -> compact detail -> full detail only when required.

**Non-Goals:**
- Do not redesign the standard full catalog again in this change.
- Do not introduce lazy-proxy-specific behavior or require a proxy.
- Do not remove the ability to retrieve full schema detail for a tool.
- Do not attempt to solve large runtime tool *result* payloads in general.

## Decisions

### 1. `detailLevel: summary` is the default tool-detail mode
The existing detail endpoint should explicitly accept `detailLevel: summary|full`, with `summary` as the default. The default detail response should be optimized for “can I likely call this tool now?” rather than “show me every schema nuance.” That means identity, concise description, and parsed arguments are the baseline, while raw schemas and other heavier detail become full-only.

For backward compatibility:
- `includeSchemas=true` is treated as equivalent to `detailLevel=full`
- `includeSchemas=false` or omitted does not force escalation
- `includeParsedArguments` remains supported
- `includeParsedArguments=false` suppresses `inputs` in both summary and full modes
- an explicit `detailLevel=full` wins over a legacy `includeSchemas=false`
- If both `detailLevel` and `includeSchemas=true` are present and conflict, `detailLevel=full` is the only valid compatible outcome
- If `detailLevel=summary` is supplied together with `includeSchemas=true`, the request should be rejected as a structured validation failure instead of silently picking one
- If `detailLevel` itself is invalid, invalid-detail-level validation takes precedence over legacy-flag conflict handling

**Alternatives considered**
- Keep today’s richer behavior as default and add a compact flag later: rejected because it keeps the common path too heavy.
- Split detail into two separate tools: rejected because the existing endpoint already supports mode flags and a single stable surface is easier to teach.

### 2. Full detail is a superset of summary
The caller should not have to relearn the response shape when escalating from summary to full. Full detail should add heavier fields, not replace the contract entirely.

**Alternatives considered**
- Make summary and full completely different payloads: rejected because it complicates client/model logic and documentation.

### 3. Summary should prefer parsed arguments over raw schema
Parsed arguments provide a better signal-to-noise ratio for models than full JSON Schema in the common case. The summary path should therefore emphasize a compact argument table and omit raw schema unless the caller explicitly requests full detail.

**Alternatives considered**
- Return reduced raw schema even in summary mode: rejected because it tends to be heavier and less directly useful than a compact argument list.

### 4. Define the field boundary explicitly
To keep the contract testable, the summary response should always include:
- `success`
- `requestedName`
- `resolvedName`
- `name`
- `title`
- `description`
- `enabled`
- `tokenCount`
- `readOnlyHint`
- `idempotentHint`
- `destructiveHint`
- `openWorldHint`
- `inputs` unless `includeParsedArguments=false`
- `failure` when unsuccessful

The full response should include all summary fields plus:
- `inputSchema`
- `outputSchema`

For this change, no additional full-only fields are introduced beyond `inputSchema` and `outputSchema`. No additional field should appear only in summary or disappear in full mode unless the design/spec is updated.

### 5. Invalid `detailLevel` values should fail explicitly
If the caller supplies any `detailLevel` other than `summary` or `full`, the endpoint should return a structured validation failure rather than silently coercing to a default. This keeps the contract predictable and testable.

The structured failure code for this case should be `invalid-detail-level`.

For the incompatible combination `detailLevel=summary` plus `includeSchemas=true`, the structured failure code should be `conflicting-detail-request`.

### 6. `tokenCount` remains a stable per-tool field
`tokenCount` should continue to represent the tool's stable per-tool metadata value, not a mode-specific count of the returned payload. This preserves backward compatibility for callers that already interpret it as tool metadata rather than response size.

### 7. Preserve the existing structured failure contract
This change refines successful detail payload sizing, but it should not weaken the structured failure behavior added in the previous change. Empty, not-found, ambiguous, and internal-error results should continue to be machine-readable in both summary and full request modes.

For failure responses, the stable contract should be:
- `success=false`
- `requestedName`
- `failure`

Failure responses should not be required to include success-path fields such as `resolvedName`, `name`, `enabled`, `tokenCount`, or `inputs`.

### 8. Documentation should explicitly model escalation
The docs should make it clear that there are three distinct levels now:
1. minimized full catalog for discovery
2. compact summary detail for normal single-tool inspection
3. full detail only when the caller needs the complete schema

This avoids regressions where clients/model prompts jump directly from discovery to full schema retrieval.

## Risks / Trade-offs

- **[Risk] Summary becomes too thin for complex tools** → Mitigation: keep parsed arguments and concise descriptive guidance in the default response, and document the escalation path clearly.
- **[Risk] Full detail and summary drift apart structurally over time** → Mitigation: define full detail as a superset of summary and cover both modes in tests.
- **[Risk] Existing callers implicitly depend on current default detail weight** → Mitigation: use an explicit `detailLevel` contract, preserve `full`, and document the default change clearly.
- **[Risk] Silent coercion of invalid detail levels hides caller bugs** → Mitigation: use a structured validation failure for unsupported `detailLevel` values.

## Migration Plan

1. Refine the Unity-side tool-detail contract so `detailLevel: summary` is the default and `detailLevel: full` is explicitly requested.
2. Add/update tests for summary-vs-full behavior, structural compatibility, invalid `detailLevel` validation, and preservation of the existing structured failure contract.
3. Update docs and OpenSpec artifacts so the recommended workflow is clear.
4. Record verification evidence under this change directory, following the same in-repo evidence approach used by the prior catalog-minimization change.
5. Verify the changed detail behavior in the same Unity test context used for the previous catalog minimization change.

Rollback is straightforward: the detail endpoint can revert to its previous default payload if the compact default proves too thin in practice.

## Open Questions

- None for implementation start. Lightweight examples are out of scope for this change, and no compatibility carve-out for defaulting to full detail is planned in this proposal.

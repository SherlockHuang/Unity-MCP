## Context

Unity-MCP currently exposes its standard MCP tool catalog through `tools/list`, which keeps it compatible with generic MCP clients but also makes the default discovery payload expensive in Codex app/CLI. The project already added a useful ingredient in the form of a Unity-side `tool-get-detail` endpoint for per-tool metadata. What is still missing is a consistent contract where the full catalog itself is discovery-oriented by default, while richer per-tool detail remains available only on demand.

This change cuts across the Unity plugin metadata surface, the server-side MCP `tools/list` shaping path, and the mirrored documentation set. It also has a compatibility constraint: standard MCP clients must continue to discover the full enabled tool set through `tools/list`.

## Goals / Non-Goals

**Goals:**
- Minimize the default `inputSchema` emitted in the standard full catalog without breaking MCP compatibility.
- Keep a strong on-demand detail path through `tool-get-detail`.

**Non-Goals:**
- Do not remove `tools/list` or replace it with a custom discovery protocol.
- Do not redesign tool signatures or flatten nested parameter structures in breaking ways.
- Do not require new client capabilities beyond standard MCP discovery and tool calls.
- Do not attempt to solve all context pressure issues caused by large runtime payloads from tool execution.

## Decisions

### 1. Minimize full-catalog schema by stripping verbose descriptions, not by changing structure
The safest smallest change is to keep standard JSON Schema shape and remove verbose descriptive payload from the default catalog representation. This preserves parameter names, required fields, and structural typing for generic clients while materially reducing token cost.

**Alternatives considered**
- Replace full input schemas with custom argument tables: rejected because it risks breaking MCP clients that expect JSON Schema.
- Remove nested structure entirely: rejected because complex Unity tool inputs would become too ambiguous to call.

### 2. Treat per-tool detail as the authoritative rich-metadata path
`tool-get-detail` is already the natural place to return parsed arguments, optional full schemas, and richer descriptive metadata. The full catalog should answer “what tools exist and what are their top-level inputs,” while `tool-get-detail` should answer “how exactly do I use this tool?”

**Alternatives considered**
- Keep embedding detailed parameter descriptions in the full catalog: rejected because it recreates the context problem.
- Move all detail logic into the lazy proxy only: rejected because the Unity server itself should provide a first-class detail endpoint that other consumers can use directly.

### 3. Minimize the standard MCP server bridge output, not the live plugin registry
The correct shaping seam is the standard MCP `tools/list` response emitted by the server bridge, not the live in-process `IRunTool` registry inside the plugin. Local/editor consumers should keep seeing rich schemas, while external MCP clients should receive the minimized discovery-oriented schema.

**Alternatives considered**
- Wrap or replace the live registered tools globally: rejected because it degrades editor-side consumers such as the tools window and helper endpoints that rely on full schema metadata.
- Minimize only helper tools like `tool-list`: rejected because it does not affect the standard MCP `tools/list` payload seen by generic clients.

### 4. Documentation must describe the two-step discovery model consistently
The root README, mirrored copies, and server/plugin docs should consistently explain that the full catalog is discovery-oriented, while `tool-get-detail` is the on-demand rich path.

## Risks / Trade-offs

- **[Risk] Generic clients rely on parameter descriptions more than expected** → Mitigation: keep schema structure intact and preserve a rich on-demand detail path.
- **[Risk] Over-minimizing nested schemas makes some complex tools harder to use directly from the full catalog** → Mitigation: preserve structural typing and required fields; move guidance, not structure, out of the catalog.
- **[Risk] Choosing the wrong shaping seam degrades local editor tooling** → Mitigation: keep the live plugin registry rich and apply minimization only to the external MCP server response.
- **[Risk] Documentation drift across mirrored README files** → Mitigation: treat README synchronization as part of the implementation and verification scope.

## Migration Plan

1. Update the standard MCP `tools/list` shaping so the default catalog emits minimized input schemas without mutating the live plugin registry.
2. Strengthen `tool-get-detail` to remain the rich-metadata endpoint with structured failure results.
3. Refresh docs and mirrored README copies to describe the new two-step workflow.
4. Run server-side/plugin verification and, where available, Unity editor tests; if Unity is unavailable locally, record that limitation explicitly.

Rollback is straightforward because the change is additive in architecture: the server can revert to the previous full-schema catalog shaping while the live plugin registry remains untouched.

## Open Questions

- Should the minimized full catalog strip only `description` fields, or should it also compact deeper schema metadata such as optional nested annotations?
- Do we want a caller-visible option to request “full catalog with full schema” for debugging, or should that remain available only through per-tool detail?

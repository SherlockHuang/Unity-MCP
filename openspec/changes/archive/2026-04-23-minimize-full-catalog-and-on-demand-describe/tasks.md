## 1. Minimize full-catalog schema output

- [x] 1.1 Trace where Unity-MCP tool `InputSchema` is finalized for the standard full catalog and identify the narrowest shared hook for schema minimization.
- [x] 1.2 Implement default full-catalog schema minimization that preserves names, required fields, and structural typing while removing verbose description payload.
- [x] 1.3 Add or update tests that verify the full catalog still exposes valid tool schemas after minimization and no longer includes the stripped descriptive payload.
- [x] 1.4 Add or update regression coverage proving local/editor-facing consumers still see rich schema metadata after full-catalog minimization.

## 2. Preserve and harden on-demand tool detail

- [x] 2.1 Update `tool-get-detail` so unknown, empty, and ambiguous lookups return machine-readable structured failure information instead of text-only exceptions.
- [x] 2.2 Add or update Unity-side tests for successful detail lookup, schema-included detail lookup, and structured invalid-name failure handling.

## 3. Sync docs and verify readiness

- [x] 3.1 Update root, server, plugin, installer, and translated README files to describe the minimized full catalog and on-demand detail workflow consistently.
- [x] 3.2 Run server-side verification and record the Unity-side execution issue that left the targeted Unity tests inconclusive in this environment.
- [x] 3.3 Validate the OpenSpec change artifacts and confirm the change is ready for `/opsx:apply` once Unity-side verification is conclusive.

## 1. Define compact summary detail as the default

- [x] 1.1 Trace the current `tool-list` and `tool-get-detail` response contracts and identify which fields are currently always returned versus optionally returned.
- [x] 1.2 Write or update failing tests that define the `detailLevel: summary|full` request contract, including the required summary/full field boundary.
- [x] 1.3 Write or update failing tests for compatibility with existing request flags (`includeSchemas`, `includeParsedArguments`) and for invalid `detailLevel` handling.
- [x] 1.4 Implement the request contract so `detailLevel: summary|full` is explicit, `summary` is the default, and the heavier fields are reserved for explicit full-detail requests.
- [x] 1.5 Ensure the full-detail mode remains a structural superset of the summary response rather than a separate incompatible shape.
- [x] 1.6 Implement the agreed field boundary so summary always returns the required compact fields and full adds only `inputSchema` and `outputSchema`.

## 2. Add regression coverage for summary versus full

- [x] 2.0 Add or update Unity-side tests proving `tool-list` returns only tool names and optional input names, and does not return/search descriptions or schemas.
- [x] 2.1 Add or update Unity-side tests for default summary detail behavior.
- [x] 2.2 Add or update Unity-side tests for explicit full-detail behavior.
- [x] 2.3 Add or update tests that compare summary and full responses to verify the expected superset relationship.
- [x] 2.4 Add or update tests for unsupported `detailLevel` values so invalid modes fail with a structured validation error rather than silent coercion.
- [x] 2.5 Carry forward regression coverage for the existing structured failure contract so summary/full refinement does not weaken empty-name, not-found, ambiguous, or internal-error behavior.
- [x] 2.6 Verify the success-path and failure-path field matrix, including the disposition of existing fields such as `requestedName` and execution hints.
- [x] 2.7 Add or update regression coverage for the legacy-flag conflict case (`detailLevel: summary` with `includeSchemas=true`) so it returns `conflicting-detail-request`.

## 3. Update docs and verify the new workflow

- [x] 3.1 Update root, plugin, server, installer, and translated README files to describe the recommended workflow: minimized full catalog -> compact detail -> full detail only when needed.
- [x] 3.2 Run verification in the appropriate Unity test context and record the results under this OpenSpec change in an auditable in-repo artifact.
- [x] 3.3 Validate the OpenSpec change artifacts and confirm the applied change remains implementation- and archive-ready.

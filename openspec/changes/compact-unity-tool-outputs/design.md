## Context

Unity-MCP already reduced catalog/discovery context cost through minimized `tools/list`, lightweight `tool-list`, and summary-first `tool-get-detail`. That does not address large payloads returned by tool execution itself. In the current client workflow, the most visible pressure comes from Unity test execution and prefab/widget reporting:

- `tests-run` stores each test result as a flat `result.Test.FullName`, so grouped runs repeat the same assembly/namespace/class prefix for every method.
- `tests-run-safe` in downstream Unity projects delegates to upstream `tests-run`, so output compaction should live upstream and be forwarded by wrappers rather than reimplemented in each project.
- Downstream prefab/widget report tools and GameObject/component inspection have related payload pressure, but they are not implemented in this first slice because the code lives partly outside this repository or already exposes explicit depth flags.

The implementation must respect the constitution: Unity API calls remain on the main thread, tool return values stay structured, nullable input handling remains explicit, tool names stay kebab-case, and tests should define the contract before implementation.

## Goals / Non-Goals

**Goals:**
- Add a compact, structured test-result format that removes repeated test-name prefixes while preserving failure diagnostics.
- Keep the existing flat test-result format available for compatibility.
- Persist the result-format choice through the existing deferred test-run path used when compilation/domain reload happens.
- Keep the compact test output inside the typed `TestRunResponse` schema so `tool-get-detail` remains truthful.
- Keep logs, stack traces, and passing-test details opt-in or explicitly requested.

**Non-Goals:**
- Do not change standard MCP catalog discovery behavior in this change.
- Do not remove or rename `tests-run`.
- Do not redesign Unity TestRunner filtering semantics.
- Do not implement downstream project-specific `tests-run-safe` changes in this repository.
- Do not define normative prefab/widget report or GameObject/component result contracts in this change.
- Do not rewrite prefab/widget report tools that live outside this repository during this implementation slice.

## Decisions

### 1. `tests-run` gains an explicit result format

Add a `resultFormat` input with a small enum, using `Flat` as the default compatibility mode and `Tree` as the compact grouped mode.

`Flat` returns the existing `TestRunResponse` behavior with `Results` populated.

`Tree` still returns `ResponseCallValueTool<TestRunResponse>`. The `TestRunResponse` type should gain typed optional fields such as:

- `ResultFormat`
- `ResultGroups`

In tree mode, `Summary` and optional `Logs` remain in the same response, `ResultGroups` is populated, and `Results` stays empty or omitted by serializer behavior. Each group hoists common identity parts such as assembly, namespace, and class, while leaf results carry method name, original full name, status, duration, and optional diagnostics.

Alternatives considered:
- Make tree the default immediately: rejected because existing callers and tests may parse `Results`.
- Return ad-hoc JSON outside `TestRunResponse`: rejected because it would make the advertised output schema inaccurate and violate the structured typed return contract.
- Return only a text summary: rejected because tool outputs must remain structured and machine-readable.
- Add a separate `tests-run-compact` tool: rejected because it splits filter/preflight/domain-reload behavior across duplicate entry points.

### 2. Preserve failure value over raw byte savings

The compact tree format should reduce repetition, not hide the information agents need to fix failures. Failed and skipped leaves should keep message text when `includeMessages=true`; stack traces should remain controlled by `includeStacktrace`; logs should remain controlled by `includeLogs`.

Passing leaves should follow `includePassingTests`. When passing tests are excluded, compact output should still rely on summary counts to prove total/passed/failed/skipped status.

Alternatives considered:
- Always include only failed method names: rejected because skipped tests and explicit passing-test requests need structured representation.
- Truncate messages by default: rejected because message text is often the highest-signal part of a failed Unity test.

### 3. Reuse existing include flags and add only one format selector

The current include flags are already understood by callers:

- `includePassingTests`
- `includeMessages`
- `includeStacktrace`
- `includeLogs`
- `logType`
- `includeLogsStacktrace`

The new result shape should reuse those flags rather than introducing parallel compact-only switches.

### 4. Persist format through deferred test runs

`tests-run` stores request and include options before `AssetDatabase.Refresh()` because a script compile/domain reload can defer execution. `resultFormat` must follow the same persistence path as the existing include flags. If a run resumes after compilation, it must produce the format requested before the refresh.

Alternatives considered:
- Store `resultFormat` only in local variables: rejected because it breaks deferred runs.
- Infer format from request ID or client: rejected because output shape must be explicit and reproducible.

### 5. Keep compact result grouping deterministic

Tree groups should be deterministic and stable for tests:

1. assembly
2. namespace
3. class/fixture
4. method leaves

If Unity does not expose assembly directly for a result, grouping may derive it from available `ITestAdaptor` metadata when collected, or leave it empty while still grouping namespace/class/method from `FullName`. The format should not invent unknown assembly names.

### 6. Leave report-style and GameObject compaction to follow-up changes

This change should focus on upstream `tests-run`, because it is in this repository and is the highest-impact result payload. Prefab/widget reports and GameObject/component inspection should be handled in later changes with their own concrete code ownership, tasks, and tests.

## Risks / Trade-offs

- **[Risk] Callers that assume `Results` always exists may fail on tree mode** -> Mitigation: keep `Flat` as the default and require callers to opt into `Tree`.
- **[Risk] Tree parsing from `FullName` misclassifies unusual test names** -> Mitigation: keep the original full name on compact leaves or expose it optionally when grouping cannot be lossless.
- **[Risk] Deferred test runs lose the requested format** -> Mitigation: persist `resultFormat` alongside existing test-run PlayerPrefs/SessionState values and cover the persistence path with tests.
- **[Risk] Compact output hides useful passing-test evidence** -> Mitigation: summary counts always remain, and `includePassingTests=true` explicitly includes passing leaves.
- **[Risk] Broad result compaction scope delays the first fix** -> Mitigation: keep this change normative only for `tests-run`; treat prefab/widget and GameObject output compaction as follow-up work.

## Migration Plan

1. Add the `TestResultFormat` request enum and typed `TestRunResponse` model fields for grouped tree output.
2. Add tests for flat compatibility, compact tree grouping, include-flag behavior, and diagnostic preservation.
3. Persist and restore `resultFormat` through the existing deferred compile/domain-reload path, with direct coverage for the persistence contract.
4. Implement tree response generation in the collector without changing Unity TestRunner execution semantics.
5. Update documentation to recommend summary-first runtime result usage and tree output for grouped test runs.
6. Validate OpenSpec artifacts and run the focused available tests.

Rollback is straightforward: callers can keep using default `Flat`, and the implementation can remove the new enum/model path without changing existing flat results.

## Open Questions

- Whether the first implementation can reliably populate assembly names for all Unity versions should be confirmed while coding. If not, the contract allows assembly to be empty while namespace/class/method grouping still removes most repeated text.

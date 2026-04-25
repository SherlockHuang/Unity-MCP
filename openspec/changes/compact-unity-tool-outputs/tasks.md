## 1. Model and Contract Tests

- [x] 1.1 Add test coverage that proves default `tests-run` output remains the existing flat `Results` shape.
- [x] 1.2 Add test coverage for compact grouped test output that hoists shared identity parts and keeps method leaves short.
- [x] 1.3 Add test coverage that compact output respects `includePassingTests`, `includeMessages`, `includeStacktrace`, `includeLogs`, `logType`, and `includeLogsStacktrace`.
- [x] 1.4 Add direct test coverage for preserving `resultFormat` through the deferred compile/domain-reload test-run persistence path.

## 2. Tests-Run Compact Result Implementation

- [x] 2.1 Add a `TestResultFormat` request enum with flat compatibility as the default and a compact grouped mode.
- [x] 2.2 Extend the typed `TestRunResponse` model with grouped result fields, including group identity fields, method leaves, original full names, status, duration, and optional diagnostics.
- [x] 2.3 Update `tests-run` to accept, validate, persist, and restore the requested result format alongside existing include flags.
- [x] 2.4 Update `TestResultCollector` to build either the existing flat response or the compact grouped response without changing Unity TestRunner execution/filter semantics.
- [x] 2.5 Keep logs and stack traces governed by the existing include flags in both flat and compact modes.

## 3. Documentation and Validation

- [x] 3.1 Update relevant README/documentation text to describe compact `tests-run` result usage separately from catalog discovery/detail usage.
- [x] 3.2 Run focused available tests for the new result models and existing test-run behavior, or document any Unity-environment limitation.
- [x] 3.3 Run `openspec validate --all --strict` and ensure the change artifacts remain apply-ready.

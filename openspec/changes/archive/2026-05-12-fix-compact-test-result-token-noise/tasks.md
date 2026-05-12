## 1. Compact Response Model

- [x] 1.1 Make tree-mode `TestRunResponse.Results` nullable/omitted while preserving flat-mode results.
- [x] 1.2 Make tree leaf `FullName` nullable fallback metadata rather than a repeated default field.

## 2. Response Builder Behavior

- [x] 2.1 Update tree response building so normal parsed leaves omit `FullName`.
- [x] 2.2 Preserve `FullName` only for unparsed or weakly parsed identities.

## 3. Tests and Validation

- [x] 3.1 Add focused tests for tree mode omitting `Results`.
- [x] 3.2 Add focused tests for normal leaves omitting `FullName` and fallback leaves preserving it.
- [x] 3.3 Run OpenSpec validation and focused automated tests.
- [x] 3.4 Run an independent agent review before commit.

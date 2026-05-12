## Context

The previous compact test-output change introduced `TestResultFormat.Tree`, `ResultGroups`, and grouped method leaves. Static review found two gaps against the token-reduction intent:

- `TestResultLeafData.FullName` repeats the full namespace/class/method string for every normal parsed leaf even though the group already carries namespace and class.
- `TestRunResponse.Results` is always initialized, so tree mode still serializes an empty legacy list in many serializers.

This follow-up keeps the public flat mode intact and only tightens the opt-in tree payload.

## Goals / Non-Goals

**Goals:**
- Avoid repeated full identities on normal tree leaves.
- Preserve fallback identity for unusual test names that cannot be decomposed into method/group fields.
- Avoid serializing an empty flat `Results` list in tree mode.
- Add focused tests for the new payload contract.

**Non-Goals:**
- Do not change Unity TestRunner execution, filtering, or deferred-run behavior.
- Do not make tree mode the default.
- Do not alter `tool-list` or `tool-get-detail`.
- Do not archive the earlier `compact-unity-tool-outputs` change in this task.

## Decisions

### 1. Make `Results` nullable and omit null values

`TestRunResponse.Results` should become nullable and use `JsonIgnoreCondition.WhenWritingNull`. Flat mode sets it to a populated list. Tree mode leaves it null and populates `ResultGroups`.

This keeps compatibility for the default mode while removing useless tree-mode bytes.

### 2. Make leaf `FullName` nullable fallback metadata

`TestResultLeafData.FullName` should become nullable and omitted when the parser can produce a method name plus a group namespace/class. It should be set only when the parsed identity is too weak to identify the test from group fields and `MethodName`.

This follows the earlier spec: tree leaves should avoid repeating full assembly/namespace/class prefixes, while still preserving enough original identity when grouping cannot resolve every part.

### 3. Keep tests at the response builder level

The response builder already isolates payload shaping from Unity TestRunner execution. Focused editor tests should validate:

- Flat mode still populates `Results`.
- Tree mode leaves `Results` null.
- Normal tree leaves have `FullName == null`.
- Fallback names preserve `FullName` when the input cannot be parsed into a normal namespace/class/method identity.

## Risks / Trade-offs

- Some tree-mode consumers may have assumed `Results` is an empty list. This is acceptable because tree mode is opt-in and documented through `ResultGroups`; flat mode remains unchanged.
- If the serializer ignores `System.Text.Json.Serialization.JsonIgnore`, null fields may still appear as explicit nulls. This is still smaller than full names and keeps the model semantically correct; tests should cover model state directly.

## Verification

- Run the focused editor/unit tests for `TestsRunResponseBuilderTests`.
- Run OpenSpec validation for the new change.
- Run an independent review agent over the resulting diff before commit.

## ADDED Requirements

### Requirement: Compact tree leaves SHALL avoid repeated full identities
When `tests-run` returns compact grouped results, method-level leaves SHALL avoid repeating the full assembly/namespace/class prefix when the group already provides enough structured identity. The original full test name SHALL be preserved only when parsing cannot provide enough group and method identity to identify the test.

#### Scenario: Parsed test identity is represented by group and method
- **WHEN** a caller invokes `tests-run` with compact grouped output
- **AND** a test name can be parsed into namespace, class, and method identity
- **THEN** the group includes the shared namespace and class
- **AND** the leaf includes the method name
- **AND** the leaf omits the full test name

#### Scenario: Test identity cannot be parsed into a normal group
- **WHEN** a compact grouped result contains a test name that cannot be decomposed into namespace, class, and method fields
- **THEN** the response still includes a structured leaf
- **AND** the leaf preserves the original full test name as fallback identity

### Requirement: Compact tree responses SHALL omit empty flat results
When `tests-run` returns compact grouped results, the response SHALL omit the legacy flat `Results` payload rather than serializing an empty list. Flat mode MUST remain the default and MUST continue to populate `Results` for compatibility.

#### Scenario: Caller requests compact tree output
- **WHEN** a caller invokes `tests-run` with compact grouped output
- **THEN** the response populates grouped result fields
- **AND** the response does not include an empty flat `Results` collection

#### Scenario: Caller uses default flat output
- **WHEN** a caller invokes `tests-run` without specifying compact grouped output
- **THEN** the response uses the existing flat result shape
- **AND** the response populates `Results` with flat test result entries

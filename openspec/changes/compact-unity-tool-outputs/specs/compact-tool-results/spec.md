## ADDED Requirements

### Requirement: Test run results SHALL support a compact grouped format
The system SHALL allow callers of `tests-run` to request a compact grouped result format that reduces repeated test identity prefixes while preserving structured test execution data. The existing flat result format MUST remain available and MUST remain the default when no format is requested. Both flat and compact grouped output MUST be represented by the typed `TestRunResponse` schema advertised by the tool.

#### Scenario: Caller omits result format
- **WHEN** a caller invokes `tests-run` without specifying a result format
- **THEN** the system returns the existing flat test result shape
- **AND** existing consumers that read the flat `Results` list continue to work

#### Scenario: Caller requests compact grouped results
- **WHEN** a caller invokes `tests-run` with the compact grouped result format
- **THEN** the response includes the test run summary
- **AND** the response populates typed grouped result fields on `TestRunResponse`
- **AND** test results are grouped by shared identity parts such as assembly, namespace, and class where available
- **AND** method-level leaves avoid repeating the full assembly/namespace/class prefix for every result
- **AND** the tool output schema remains accurate for the response fields returned by compact mode

#### Scenario: Caller compares flat and compact schema
- **WHEN** a caller inspects tool detail for `tests-run`
- **THEN** the advertised output schema includes the typed fields used by both flat and compact result formats
- **AND** compact mode does not require parsing an untyped JSON blob outside the declared response model

#### Scenario: Grouping cannot resolve every identity part
- **WHEN** Unity test metadata does not provide a reliable assembly, namespace, class, or method part
- **THEN** the compact response still returns a structured result
- **AND** it preserves enough original identity information for the caller to identify the test

### Requirement: Compact test results SHALL preserve diagnostics according to existing include flags
The compact grouped result format SHALL respect the existing test output flags for passing tests, messages, stack traces, and logs. Compact output MUST reduce repetition without hiding explicitly requested diagnostics.

#### Scenario: Passing tests are omitted by default
- **WHEN** a caller requests compact grouped results and does not enable passing-test details
- **THEN** passing test leaves are omitted from the grouped result details
- **AND** the summary still reports total and passed test counts

#### Scenario: Caller requests passing test details
- **WHEN** a caller requests compact grouped results with passing-test details enabled
- **THEN** passing test leaves are included in the grouped result details

#### Scenario: Caller requests messages for failed tests
- **WHEN** a failed or skipped test appears in compact grouped results and messages are enabled
- **THEN** the leaf result includes the test message when Unity provides one

#### Scenario: Caller requests stack traces
- **WHEN** a caller enables stack traces for compact grouped results
- **THEN** failing leaves include stack traces when Unity provides them
- **AND** stack traces remain omitted when stack traces are not enabled

#### Scenario: Caller requests logs
- **WHEN** a caller enables logs for compact grouped results
- **THEN** log entries are included using the existing log level and stack trace filters
- **AND** logs remain omitted by default

### Requirement: Deferred test runs SHALL preserve requested result format
The system SHALL preserve the requested test result format when a `tests-run` call is deferred by script compilation, asset refresh, or Unity domain reload. A resumed run MUST use the same result format and include flags that were requested before deferral.

#### Scenario: Test run resumes after compilation
- **WHEN** a caller starts `tests-run` with compact grouped output and Unity defers the run until after compilation
- **THEN** the completed response uses compact grouped output
- **AND** the response does not silently fall back to the default flat format

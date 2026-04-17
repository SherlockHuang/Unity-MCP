## MODIFIED Requirements

### Requirement: Tool detail SHALL default to a compact summary payload
The system SHALL provide a default tool-detail response that is intentionally compact and optimized for model comprehension and invocation planning. The detail request contract SHALL use `detailLevel: summary|full`, where `summary` is the default when the caller omits the field. The default summary MUST avoid returning the heaviest schema payload unless the caller explicitly asks for it.

For backward compatibility:
- `includeSchemas=true` SHALL be treated as equivalent to requesting `detailLevel: full`
- `includeSchemas=false` or omission SHALL NOT force escalation
- `includeParsedArguments` SHALL remain supported
- `includeParsedArguments=false` SHALL suppress `inputs` in both summary and full modes
- an explicit `detailLevel: full` SHALL win over a legacy `includeSchemas=false`
- a request that combines `detailLevel: summary` with `includeSchemas=true` SHALL be rejected as a structured validation failure
- if `detailLevel` itself is invalid, invalid-detail-level validation SHALL take precedence over legacy-flag conflict handling

#### Scenario: Caller requests default tool detail
- **WHEN** a caller requests tool detail without setting `detailLevel`
- **THEN** the system returns a compact summary response
- **AND** the summary includes the tool identity, concise description, and a machine-readable argument list sufficient for invocation planning
- **AND** the summary omits full input and output schemas by default

#### Scenario: Caller requests summary detail for a complex tool
- **WHEN** the caller sets `detailLevel` to `summary` for a tool with nested object or array arguments
- **THEN** the summary still exposes enough top-level argument information for the caller to decide whether more detail is needed
- **AND** the response avoids embedding the full nested schema unless explicitly requested

### Requirement: Tool detail SHALL support an explicit full payload mode
The system SHALL support an explicit full-detail mode for callers that need the complete schema and richer guidance for a single tool. This mode MUST be opt-in and MUST preserve the richer metadata that was intentionally removed from the compact summary path.

#### Scenario: Caller requests full detail
- **WHEN** a caller sets `detailLevel` to `full` for a tool
- **THEN** the system returns the tool's richer schema metadata
- **AND** the full response remains limited to the selected tool rather than expanding the default full catalog

### Requirement: Summary and full detail SHALL have a predictable contract
The system SHALL define a stable distinction between summary fields and full-only fields so callers can reliably decide when they need escalation from summary to full. The summary contract MUST remain useful on its own, and the full contract MUST be a strict superset rather than a different or loosely expanded shape.

The summary response SHALL include:
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

The full response SHALL include all summary fields and SHALL additionally include:
- `inputSchema`
- `outputSchema`

No other full-only fields are introduced by this change.

#### Scenario: Caller compares summary and full responses
- **WHEN** a caller requests summary detail and then requests full detail for the same tool
- **THEN** the full response includes the summary fields in a compatible structure
- **AND** `inputSchema` and `outputSchema` are the only additional heavy fields introduced by this change

### Requirement: Unsupported detail levels SHALL fail with a structured validation error
The system SHALL reject any `detailLevel` value other than `summary` or `full` with a structured validation failure. It SHALL NOT silently coerce unsupported values to the default mode.

#### Scenario: Caller passes an unsupported detail level
- **WHEN** a caller provides a `detailLevel` value other than `summary` or `full`
- **THEN** the system returns a structured validation failure
- **AND** the failure code is `invalid-detail-level`

#### Scenario: Caller passes a conflicting summary request
- **WHEN** a caller provides `detailLevel: summary` together with `includeSchemas=true`
- **THEN** the system returns a structured validation failure
- **AND** the failure code is `conflicting-detail-request`
- **AND** the response indicates that the request flags conflict rather than claiming `summary` itself is unsupported

### Requirement: Structured failure behavior SHALL be preserved
The system SHALL preserve the existing structured failure behavior for tool detail lookups regardless of whether the caller uses summary or full mode. This includes empty-name, not-found, ambiguous, and internal-error result shapes.

For failure responses, the stable contract SHALL include:
- `success=false`
- `requestedName`
- `failure`

Failure responses SHALL NOT be required to include success-path fields such as `resolvedName`, `name`, `enabled`, `tokenCount`, or `inputs`.

#### Scenario: Caller requests summary detail for an unknown tool
- **WHEN** a caller requests `detailLevel: summary` for a tool name that does not exist
- **THEN** the system returns the same structured lookup failure contract used by the existing detail endpoint
- **AND** the response does not degrade into text-only exceptions

#### Scenario: Caller requests full detail for an internal failure case
- **WHEN** a caller requests `detailLevel: full` and the endpoint hits an internal failure
- **THEN** the system returns the same structured internal-error contract used by the existing detail endpoint
- **AND** the client-facing message remains sanitized

### Requirement: Documentation SHALL guide callers toward summary-first usage
The system SHALL document the intended discovery workflow so callers start with the minimized full catalog, escalate to compact summary detail next, and request the full detail payload only when necessary.

#### Scenario: Reader follows documented workflow
- **WHEN** a developer or model reads the updated documentation
- **THEN** the documentation explains that compact summary detail is the default next step after full-catalog discovery
- **AND** it explains that `detailLevel: full` should be requested only when the summary contract is insufficient

# compact-tool-catalog Specification

## Purpose
Define the discovery-first tool catalog contract for Unity-MCP, including minimized `tools/list` schemas, lightweight `tool-list` discovery, and on-demand `tool-get-detail` escalation from summary metadata to full schemas.

## Requirements
### Requirement: Full catalog SHALL expose a minimized default input schema
The system SHALL keep the standard MCP full tool catalog discoverable through `tools/list`, but the default `inputSchema` exposed for Unity-MCP tools MUST be minimized for discovery rather than full instructional detail. The minimized schema MUST preserve field names, requiredness, and enough type structure for basic invocation planning, while omitting verbose descriptive payload that is not necessary for discovery.

#### Scenario: Client performs standard tool discovery
- **WHEN** a standard MCP client requests `tools/list`
- **THEN** the Unity-MCP server returns the full set of enabled tools
- **AND** each tool's default `inputSchema` preserves parameter names, required fields, and core type structure
- **AND** verbose property-level schema description content is omitted from the default full-catalog response

#### Scenario: Complex object inputs remain structurally discoverable
- **WHEN** a tool accepts nested object or array inputs
- **THEN** the minimized default `inputSchema` still exposes the parameter's structural shape needed for invocation planning
- **AND** the response avoids including unnecessary descriptive text for nested fields in the default full catalog

### Requirement: Tool detail SHALL remain available on demand
The system SHALL provide an explicit per-tool detail path so models and clients can request richer tool metadata only when needed. This detail path MUST support advanced argument inspection and optional full schema retrieval without requiring the default full catalog to carry the same level of detail.

#### Scenario: Caller requests summary detail for one tool
- **WHEN** a caller requests tool detail without asking for full schema payload
- **THEN** the system returns machine-readable tool metadata sufficient to understand the tool's purpose and argument list
- **AND** the result avoids returning full input and output schemas by default

#### Scenario: Caller requests full schema detail for one tool
- **WHEN** a caller explicitly requests full schema detail for a single tool
- **THEN** the system returns that tool's detailed schema and metadata
- **AND** the full-catalog minimization remains unchanged for all other tools

### Requirement: Full-catalog minimization SHALL not degrade local rich-schema consumers
The system SHALL apply full-catalog schema minimization at the external MCP catalog exposure boundary rather than by mutating the live in-process tool registry. Local plugin/editor consumers and rich helper endpoints MUST retain access to full tool schema metadata.

#### Scenario: Editor consumer reads the live tool registry
- **WHEN** a local plugin or editor UI reads tool metadata directly from the live tool registry
- **THEN** it still receives the full rich schema metadata
- **AND** only the standard external MCP full-catalog response is minimized

### Requirement: Per-tool detail errors SHALL be structured
The system SHALL return machine-readable failure information when a caller requests detail for an invalid or unsupported tool name. This requirement applies to Unity-side per-tool detail lookup and MUST distinguish unsupported capability from normal validation failures.

#### Scenario: Caller requests detail for an unknown tool
- **WHEN** a caller requests detail for a tool name that does not exist
- **THEN** the system returns a structured failure payload that indicates the lookup failed
- **AND** the result does not require consumers to parse free-form exception text to determine the outcome

#### Scenario: Detail endpoint fails internally
- **WHEN** the per-tool detail endpoint exists but fails due to an internal error
- **THEN** the caller receives a structured internal-error result
- **AND** consumers do not misclassify the failure as “endpoint unavailable”

### Requirement: Tool list SHALL remain a lightweight discovery helper
The Unity-side `tool-list` helper SHALL be limited to discovering candidate tools. By default it SHALL return tool names only. When the caller requests `includeInputs: Inputs`, it SHALL additionally return input names only. It SHALL NOT return tool descriptions, input descriptions, full input schemas, or output schemas.

Filtering SHALL stay aligned with discovery: `regexSearch` SHALL match tool names and input names only. It SHALL NOT match tool descriptions or input descriptions.

#### Scenario: Caller lists tools without inputs
- **WHEN** a caller invokes `tool-list` without `includeInputs`
- **THEN** the result includes each candidate tool's `name`
- **AND** the result omits tool descriptions
- **AND** the result omits input and output schemas

#### Scenario: Caller lists tools with input names
- **WHEN** a caller invokes `tool-list` with `includeInputs: Inputs`
- **THEN** each returned input entry includes its `name`
- **AND** each returned input entry omits its description
- **AND** no full schema payload is returned

#### Scenario: Caller filters by input name
- **WHEN** a caller invokes `tool-list` with a `regexSearch` value that matches an input name
- **THEN** tools containing that input name are returned as candidates

#### Scenario: Caller filters by description text
- **WHEN** a caller invokes `tool-list` with a `regexSearch` value that matches only tool or input description text
- **THEN** those descriptions do not cause a match
- **AND** the caller can use `tool-get-detail` to inspect descriptions for a selected tool

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
The system SHALL document the intended discovery workflow so callers start with the minimized full catalog or `tool-list`, escalate to compact summary detail next, and request the full detail payload only when necessary.

#### Scenario: Reader follows documented workflow
- **WHEN** a developer or model reads the updated documentation
- **THEN** the documentation explains that compact summary detail is the default next step after full-catalog or `tool-list` discovery
- **AND** it explains that `detailLevel: full` should be requested only when the summary contract is insufficient

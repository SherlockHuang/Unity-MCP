## ADDED Requirements

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

## ADDED Requirements

### Requirement: Package dependencies do not pollute Assets
Unity-MCP SHALL avoid restoring or copying NuGet dependency DLLs into a consuming project's `Assets/Plugins/NuGet` directory during normal package import.

#### Scenario: Package import with declared dependencies
- **WHEN** a Unity project imports Unity-MCP through the supported UPM/OpenUPM manifest path
- **THEN** Unity-MCP dependency DLLs are resolved through `Packages/` and `Packages/packages-lock.json` rather than `Assets/Plugins/NuGet`

### Requirement: Unity-MCP-owned DLLs are bundled in the package
Unity-MCP SHALL include the Unity-MCP-owned binary dependencies that are not fully covered by OpenUPM wrappers inside the Unity-MCP UPM package.

#### Scenario: Packed package contains owned DLLs
- **WHEN** the Unity-MCP package is packed or published
- **THEN** `McpPlugin.dll`, `McpPlugin.Common.dll`, and `ReflectorNet.dll` are present under the package's `Plugins/NuGet` tree

### Requirement: OpenUPM wrapper DLLs are configured before gated assemblies compile
Unity-MCP SHALL configure importer settings for required bundled DLLs and required OpenUPM wrapper DLLs before setting `UNITY_MCP_OPENUPM_READY`.

#### Scenario: Resolver configures package DLL importers
- **WHEN** the dependency resolver runs in the Unity Editor
- **THEN** required DLLs referenced by Unity-MCP assemblies are editor-compatible according to their configured runtime/editor inclusion policy before `UNITY_MCP_OPENUPM_READY` is set

### Requirement: Installer-managed manifests include required OpenUPM scopes
Unity-MCP SHALL ensure installer-managed project manifests include OpenUPM scopes required to resolve Unity-MCP and its `org.nuget.*` dependencies.

#### Scenario: Installer updates manifest scopes
- **WHEN** the installer updates a consuming project's `Packages/manifest.json`
- **THEN** the OpenUPM scoped registry includes `com.ivanmurzak`, `extensions.unity`, `org.nuget.microsoft`, `org.nuget.system`, and `org.nuget.r3`

### Requirement: Direct installs state registry prerequisites
Unity-MCP SHALL treat direct Git, tarball, or manual package installs as requiring the consuming project to already define the OpenUPM scopes needed by Unity-MCP dependencies.

#### Scenario: Direct install prerequisites
- **WHEN** a user installs Unity-MCP without the installer or another project-manifest manager
- **THEN** the user-facing package guidance identifies the required OpenUPM scopes before relying on `org.nuget.*` dependencies

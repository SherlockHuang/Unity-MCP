## Why

The package resolver is moving away from downloading NuGet DLLs into consuming projects under `Assets/Plugins/NuGet`, but the hybrid UPM/OpenUPM approach still needs a reliable import and install contract. Without that contract, OpenUPM wrapper DLLs may remain unavailable to Editor assemblies, and direct install paths may fail to resolve `org.nuget.*` dependencies.

## What Changes

- Keep Unity-MCP-owned DLLs (`McpPlugin.dll`, `McpPlugin.Common.dll`, `ReflectorNet.dll`) bundled inside the Unity-MCP UPM package.
- Resolve third-party NuGet wrapper dependencies through OpenUPM package dependencies instead of copying them into `Assets/Plugins/NuGet`.
- Harden dependency configuration so OpenUPM wrapper DLLs required by Unity-MCP are Editor-compatible before `UNITY_MCP_OPENUPM_READY` enables gated assemblies.
- Make the supported install paths explicit: installer/manifest-managed installs must add the OpenUPM scopes required for `org.nuget.*`; direct Git/tarball/manual installs require the consuming project to already have those scopes.
- Release the fixed package as `0.66.2`.

## Capabilities

### New Capabilities
- `upm-nuget-dependency-resolution`: Defines how Unity-MCP resolves bundled and OpenUPM NuGet DLLs without polluting consuming project `Assets/`.

### Modified Capabilities

## Impact

- `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/package.json`
- `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/Editor/DependencyResolver/`
- Unity package manifests for the plugin and test projects
- Installer manifest mutation and expected-manifest tests
- Release/version files updated by `commands/bump-version.ps1`
- Release packaging and local GitLab release assets for `0.66.2`

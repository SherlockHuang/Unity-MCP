## Context

Unity-MCP previously restored NuGet packages into each Unity project under `Assets/Plugins/NuGet`, then configured the resulting DLL `PluginImporter` settings before setting `UNITY_MCP_READY`. The new packaging direction removes project pollution by bundling only Unity-MCP-owned DLLs inside the UPM package and declaring third-party NuGet wrappers as OpenUPM dependencies.

The first-pass implementation exposed two risks:

- OpenUPM wrapper DLLs live outside `Packages/com.ivanmurzak.unity.mcp`, so scanning only Unity-MCP's bundled DLL directory does not configure wrapper DLL importer settings.
- `scopedRegistries` must be present in the consuming project's `Packages/manifest.json`; a package's own manifest cannot guarantee registry availability for direct Git/tarball/manual installs.

## Goals / Non-Goals

**Goals:**

- Keep consuming project `Assets/` free of Unity-MCP NuGet DLL restores.
- Bundle `McpPlugin.dll`, `McpPlugin.Common.dll`, and `ReflectorNet.dll` inside the Unity-MCP UPM package.
- Resolve standard `org.nuget.*` dependencies through OpenUPM package dependencies.
- Configure required DLL importer settings for both bundled Unity-MCP DLLs and OpenUPM wrapper DLLs before setting `UNITY_MCP_OPENUPM_READY`.
- Preserve installer-managed installs by adding all required OpenUPM scopes to the project manifest.
- Release the fixed package as `0.66.2`.

**Non-Goals:**

- Reintroducing automatic NuGet download/extract behavior.
- Writing third-party dependencies to `Assets/Plugins/NuGet`.
- Guaranteeing direct Git/tarball/manual installs without a consuming project manifest that already includes the required OpenUPM scopes.

## Decisions

1. **Use UPM/OpenUPM dependency declarations for third-party NuGet wrappers.**

   The package manifest remains the source of dependency versions. This lets Unity resolve wrapper packages into `Packages/packages-lock.json` and avoids project `Assets/` pollution.

2. **Keep Unity-MCP-owned DLLs bundled in the package.**

   `com.IvanMurzak.McpPlugin` has no OpenUPM wrapper package, and bundling `ReflectorNet` with it keeps the core Unity-MCP binary dependency set under package control.

3. **Configure importer settings through AssetDatabase-discovered assets, not raw filesystem enumeration alone.**

   Unity registry packages and package-cache content may not be reliably discoverable through the `Packages/<package-name>` filesystem path. The resolver should enumerate Unity asset paths for configured package folders and call `PluginImporter` on those asset paths.

4. **Treat OpenUPM registry scopes as project manifest state.**

   Installer-managed installs must add `com.ivanmurzak`, `extensions.unity`, `org.nuget.microsoft`, `org.nuget.system`, and `org.nuget.r3` to the OpenUPM scoped registry. Direct installs are documented/validated as requiring those scopes up front.

## Risks / Trade-offs

- **OpenUPM wrapper packages have unexpected importer metadata** -> The resolver explicitly reconfigures required DLLs before enabling gated assemblies.
- **Unity disallows modifying importer settings in package-cache assets for some package sources** -> Verification must include local package import and dry-run package contents; if a Unity Editor import proves package-cache importer changes are blocked, the fallback is to bundle the affected DLLs inside Unity-MCP's package instead of relying on wrapper metadata.
- **Direct Git/tarball installs fail dependency resolution before Unity-MCP code can run** -> Installer and documented manifest-managed paths are the supported automated path; direct installs require user-managed OpenUPM scopes.
- **Version bump/release misses server assets** -> Use the existing bump script and release packaging workflow, then inspect server zip contents before publishing.

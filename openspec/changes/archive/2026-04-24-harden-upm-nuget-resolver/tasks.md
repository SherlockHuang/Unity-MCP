## 1. Resolver Hardening

- [x] 1.1 Update dependency resolver configuration so it can classify Unity-MCP bundled DLLs and declared OpenUPM wrapper DLLs.
- [x] 1.2 Update plugin importer configuration to discover package DLLs through Unity asset paths and configure required OpenUPM wrapper DLLs before `UNITY_MCP_OPENUPM_READY`.
- [x] 1.3 Remove stale downloader/restorer code paths and confirm no runtime path writes NuGet DLLs to `Assets/Plugins/NuGet`.

## 2. Manifest and Install Contract

- [x] 2.1 Keep Unity-MCP package dependencies and test manifests aligned with the required OpenUPM wrapper packages and scopes.
- [x] 2.2 Keep installer manifest mutation, CLI manifest mutation (`cli/src/utils/manifest.ts`), and their expected manifest tests aligned with required OpenUPM scopes.
- [x] 2.3 Document direct Git/tarball/manual install prerequisites in the package-facing installation docs, including the required OpenUPM scoped registry snippet for `com.ivanmurzak`, `extensions.unity`, `org.nuget.microsoft`, `org.nuget.system`, and `org.nuget.r3`.

## 3. Release and Verification

- [x] 3.1 Bump the project version to `0.66.2` using the repository version bump script.
- [x] 3.2 Run a Unity Editor import/domain reload verification that proves required bundled and OpenUPM wrapper DLL importers are configured before `UNITY_MCP_OPENUPM_READY` enables gated assemblies.
- [x] 3.3 Run automated verification for JSON/package metadata, OpenUPM package availability, release packaging, server tests, CLI build/tests, and package archive contents.
- [x] 3.4 Archive the OpenSpec change after implementation and verification pass.
- [ ] 3.5 Commit the scoped resolver/release changes and publish the `0.66.2` release assets.

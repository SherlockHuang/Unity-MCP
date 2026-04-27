/*
+------------------------------------------------------------------+
|  Author: Ivan Murzak (https://github.com/IvanMurzak)             |
|  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    |
|  Copyright (c) 2025 Ivan Murzak                                  |
|  Licensed under the Apache License, Version 2.0.                 |
|  See the LICENSE file in the project root for more information.   |
+------------------------------------------------------------------+
*/

#nullable enable

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Configuration for dependency PluginImporter settings.
    /// Third-party NuGet packages are resolved by OpenUPM wrapper dependencies.
    /// </summary>
    static class NuGetConfig
    {
        /// <summary>Log tag shared across all DependencyResolver classes.</summary>
        public const string LogTag = "[NuGet]";

        /// <summary>Unity-MCP UPM package name.</summary>
        public const string PackageName = "com.ivanmurzak.unity.mcp";

        /// <summary>OpenUPM NuGet wrapper package prefix.</summary>
        public const string OpenUpmNuGetPackagePrefix = "org.nuget.";

        /// <summary>
        /// Where bundled Unity-MCP-owned DLLs live inside the UPM package.
        /// Third-party NuGet DLLs are resolved by OpenUPM wrapper packages.
        /// </summary>
        public const string InstallPath = "Packages/" + PackageName + "/Plugins/NuGet";

        /// <summary>
        /// NuGet package IDs used to classify DLL importer settings when a DLL folder
        /// follows the {packageId}.{version} naming convention.
        ///
        /// includeInBuild: true  = DLL included in game builds (runtime dependency)
        /// includeInBuild: false = editor-only DLL (excluded from builds)
        /// </summary>
        public static readonly NuGetPackage[] Packages =
        {
            // --- Runtime dependencies (included in game builds) ---
            // Keep these in sync with package.json so the OpenUPM-resolved DLLs
            // match the assembly definition precompiled references.
            new NuGetPackage("System.Text.Json",                                      "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Client",                   "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.AspNetCore.SignalR.Protocols.Json",           "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging",                          "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Logging.Abstractions",             "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.DependencyInjection",              "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.DependencyInjection.Abstractions", "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Options",                          "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Caching.Abstractions",             "10.0.3", includeInBuild: true),
            new NuGetPackage("Microsoft.Extensions.Hosting.Abstractions",             "10.0.3", includeInBuild: true),
            new NuGetPackage("R3",                                                    "1.3.0",  includeInBuild: true),

            // --- Editor-only dependencies (excluded from builds) ---
            new NuGetPackage("Microsoft.Bcl.Memory",                                  "10.0.3"),
            new NuGetPackage("Microsoft.CodeAnalysis.CSharp",                         "4.14.0"),
        };

        public static bool IsManagedOpenUpmPackageName(string packageName)
            => packageName.StartsWith(OpenUpmNuGetPackagePrefix, System.StringComparison.OrdinalIgnoreCase);

        public static bool IsUnityMcpDependencyPackageName(string packageName)
            => GetPackageByOpenUpmName(packageName) != null;

        public static NuGetPackage? GetPackageByOpenUpmName(string packageName)
        {
            foreach (var package in Packages)
            {
                if (string.Equals(package.OpenUpmPackageName, packageName, System.StringComparison.OrdinalIgnoreCase))
                    return package;
            }
            return null;
        }

    }
}

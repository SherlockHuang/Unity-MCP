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
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Configures PluginImporter settings for NuGet DLLs.
    ///
    /// Handles four cases:
    ///   1. Unity provides the DLL + we need it in builds → include in builds, exclude from editor
    ///   2. Unity provides the DLL + editor-only → disable entirely
    ///   3. We provide the DLL + we need it in builds → include everywhere
    ///   4. We provide the DLL + editor-only → editor only
    ///
    /// Case 1 is critical: assemblies like System.Diagnostics.DiagnosticSource are
    /// available in the Unity Editor but NOT included in player builds automatically.
    /// Our NuGet copy must be included in builds while excluded from editor to avoid duplicates.
    /// </summary>
    static class NuGetPluginConfigurator
    {
        const string Tag = NuGetConfig.LogTag;

        /// <summary>
        /// Configures PluginImporter for all DLLs in the NuGet install directory.
        /// Called after packages are installed/restored.
        /// </summary>
        public static int ConfigureAll()
        {
            var dlls = FindDependencyDllAssetPaths();
            if (dlls.Count == 0)
                return 0;

            var configuredCount = 0;
            // Batch importer changes so Unity performs a single reimport pass at the end
            // instead of one reimport per DLL (which was dominating editor startup time
            // on projects with many NuGet packages).
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var dllPath in dlls)
                {
                    if (ConfigureDll(dllPath))
                        configuredCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return HasRequiredOpenUpmDllsConfigured() ? configuredCount : 0;
        }

        static List<string> FindDependencyDllAssetPaths()
        {
            var result = new List<string>();

            foreach (var package in PackageInfo.GetAllRegisteredPackages())
            {
                if (string.Equals(package.name, NuGetConfig.PackageName, System.StringComparison.OrdinalIgnoreCase))
                {
                    var resolvedBundledPath = string.IsNullOrEmpty(package.resolvedPath)
                        ? null
                        : Path.Combine(package.resolvedPath, "Plugins", "NuGet");
                    AddDllsFromPackageFolder(result, NuGetConfig.InstallPath, resolvedBundledPath, includeOnlyLibDlls: false);
                    continue;
                }

                if (!NuGetConfig.IsUnityMcpDependencyPackageName(package.name))
                    continue;

                var packageRoot = $"Packages/{package.name}";
                AddDllsFromPackageFolder(result, packageRoot, package.resolvedPath, includeOnlyLibDlls: true);
            }

            return result;
        }

        static void AddDllsFromPackageFolder(List<string> result, string assetRoot, string? resolvedRoot, bool includeOnlyLibDlls)
        {
            foreach (var guid in AssetDatabase.FindAssets("", new[] { assetRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                AddDllIfNeeded(result, assetPath, includeOnlyLibDlls);
            }

            if (string.IsNullOrEmpty(resolvedRoot) || !Directory.Exists(resolvedRoot))
                return;

            var physicalRoot = resolvedRoot!;
            foreach (var filePath in Directory.GetFiles(physicalRoot, "*.dll", SearchOption.AllDirectories))
            {
                var relativePath = filePath.Substring(physicalRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var assetPath = (assetRoot.TrimEnd('/') + "/" + relativePath).Replace('\\', '/');
                AddDllIfNeeded(result, assetPath, includeOnlyLibDlls);
            }
        }

        static void AddDllIfNeeded(List<string> result, string assetPath, bool includeOnlyLibDlls)
        {
            if (!assetPath.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
                return;

            if (includeOnlyLibDlls && assetPath.IndexOf("/lib/", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            if (!result.Contains(assetPath))
                result.Add(assetPath);
        }

        /// <summary>
        /// Configures a single DLL's PluginImporter settings.
        /// </summary>
        public static bool ConfigureDll(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
            }

            if (importer == null)
                return false;

            var dllName = Path.GetFileNameWithoutExtension(assetPath);
            var unityProvidesIt = UnityAssemblyResolver.IsAlreadyImported(dllName);
            var includeInBuild = ShouldIncludeInBuild(assetPath);

            bool anyPlatform;
            bool excludeEditor;
            bool editorOnly;

            if (unityProvidesIt && includeInBuild)
            {
                // Unity provides this DLL in the editor, but builds need our copy.
                anyPlatform = true;
                excludeEditor = true;
                editorOnly = false;
            }
            else if (unityProvidesIt)
            {
                // Unity provides it and we don't need it in builds — disable entirely.
                anyPlatform = false;
                excludeEditor = false;
                editorOnly = false;
            }
            else if (includeInBuild)
            {
                // Runtime DLL not provided by Unity: include everywhere.
                anyPlatform = true;
                excludeEditor = false;
                editorOnly = false;
            }
            else
            {
                // Editor-only DLL not provided by Unity.
                anyPlatform = false;
                excludeEditor = false;
                editorOnly = true;
            }

            // Check if settings need to change
            var currentAnyPlatform = importer.GetCompatibleWithAnyPlatform();
            var currentEditor = importer.GetCompatibleWithEditor();
            var currentExcludeEditor = importer.GetExcludeEditorFromAnyPlatform();
            var currentExplicitlyReferenced = IsExplicitlyReferenced(assetPath);

            // OpenUPM wrapper packages may already select the right TFM/platform using
            // defineConstraints (for example UNITY_EDITOR or UNITY_2021_2_OR_NEWER).
            // For editor-only wrappers, do not fight those platform settings; only make
            // the DLL explicitly referenceable so asmdefs can name it.
            if (!includeInBuild
                && IsManagedOpenUpmPackageAssetPath(assetPath)
                && currentAnyPlatform
                && !currentExcludeEditor)
            {
                if (!currentExplicitlyReferenced)
                {
                    EnsureExplicitlyReferenced(assetPath);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
                return true;
            }

            // When Any Platform is on, Editor compatibility is governed by Exclude Editor.
            // Unity keeps the individual Editor flag disabled in that mode, so checking it
            // directly would make this resolver reimport the DLL forever.
            var expectedEditor = anyPlatform ? !excludeEditor : editorOnly;
            var editorMatches = anyPlatform || currentEditor == expectedEditor;
            var needsChange = currentAnyPlatform != anyPlatform
                           || currentExcludeEditor != excludeEditor
                           || !editorMatches
                           || !currentExplicitlyReferenced;

            if (!needsChange)
                return true;

            if (anyPlatform)
            {
                importer.SetCompatibleWithAnyPlatform(true);
                importer.SetExcludeEditorFromAnyPlatform(excludeEditor);
                // Explicitly sync the individual Editor platform flag. Unity's initial import
                // sometimes leaves Editor at enabled=0 even when Any Platform is on without
                // Exclude Editor; without this call, the stale 0 persists in the .meta and
                // Editor-side loading fails (e.g., "Unloading broken assembly ..." for DLLs
                // whose transitive deps are also editor-disabled).
                importer.SetCompatibleWithEditor(!excludeEditor);
            }
            else
            {
                importer.SetCompatibleWithAnyPlatform(false);
                importer.SetCompatibleWithEditor(editorOnly);
            }

            importer.SaveAndReimport();
            EnsureExplicitlyReferenced(assetPath);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"{Tag} Configured '{dllName}': anyPlatform={anyPlatform}, excludeEditor={excludeEditor}, editorOnly={editorOnly}");
            return true;
        }

        static bool HasRequiredOpenUpmDllsConfigured()
        {
            foreach (var package in NuGetConfig.Packages)
            {
                if (!HasConfiguredDllForPackage(package.OpenUpmPackageName))
                    return false;
            }
            return true;
        }

        static bool HasConfiguredDllForPackage(string packageName)
        {
            var packageRoot = $"Packages/{packageName}";
            foreach (var guid in AssetDatabase.FindAssets("t:PluginImporter", new[] { packageRoot }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
                if (assetPath.IndexOf("/lib/", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer == null)
                    continue;

                if (IsEditorCompatible(importer) && IsExplicitlyReferenced(assetPath))
                    return true;
            }

            var package = PackageInfo.FindForAssetPath(packageRoot);
            if (package == null || string.IsNullOrEmpty(package.resolvedPath) || !Directory.Exists(package.resolvedPath))
                return false;

            foreach (var filePath in Directory.GetFiles(package.resolvedPath, "*.dll", SearchOption.AllDirectories))
            {
                var relativePath = filePath.Substring(package.resolvedPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var assetPath = (packageRoot + "/" + relativePath).Replace('\\', '/');
                if (assetPath.IndexOf("/lib/", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer != null && IsEditorCompatible(importer) && IsExplicitlyReferenced(assetPath))
                    return true;
            }

            return false;
        }

        static bool IsEditorCompatible(PluginImporter importer)
        {
            return importer.GetCompatibleWithAnyPlatform()
                ? !importer.GetExcludeEditorFromAnyPlatform()
                : importer.GetCompatibleWithEditor();
        }

        static bool IsExplicitlyReferenced(string assetPath)
        {
            var metaPath = ResolveMetaPath(assetPath);
            if (metaPath == null || !File.Exists(metaPath))
                return false;

            return File.ReadAllText(metaPath).Contains("isExplicitlyReferenced: 1");
        }

        static void EnsureExplicitlyReferenced(string assetPath)
        {
            var metaPath = ResolveMetaPath(assetPath);
            if (metaPath == null || !File.Exists(metaPath))
                return;

            var content = File.ReadAllText(metaPath);
            if (content.Contains("isExplicitlyReferenced: 1"))
                return;

            content = content.Contains("isExplicitlyReferenced: 0")
                ? content.Replace("isExplicitlyReferenced: 0", "isExplicitlyReferenced: 1")
                : content.Replace("validateReferences:", "isExplicitlyReferenced: 1\n  validateReferences:");
            File.WriteAllText(metaPath, content);
        }

        static string? ResolveMetaPath(string assetPath)
        {
            var normalized = assetPath.Replace('\\', '/');
            const string prefix = "Packages/";
            if (!normalized.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return null;

            var slash = normalized.IndexOf('/', prefix.Length);
            if (slash < 0)
                return null;

            var packageName = normalized.Substring(prefix.Length, slash - prefix.Length);
            var package = PackageInfo.FindForAssetPath($"Packages/{packageName}");
            if (package == null || string.IsNullOrEmpty(package.resolvedPath))
                return null;

            var relativePath = normalized.Substring(slash + 1).Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(package.resolvedPath, relativePath) + ".meta";
        }

        /// <summary>
        /// Determines if a DLL should be included in game builds.
        /// Explicitly configured packages use their IncludeInBuild flag.
        /// Transitive dependencies default to included (runtime packages depend on them).
        /// </summary>
        static bool ShouldIncludeInBuild(string dllPath)
        {
            var packageName = ExtractPackageNameFromAssetPath(dllPath);
            if (packageName != null && NuGetConfig.IsManagedOpenUpmPackageName(packageName))
                return NuGetConfig.GetPackageByOpenUpmName(packageName)?.IncludeInBuild ?? true;

            var dirName = Path.GetFileName(Path.GetDirectoryName(dllPath));
            if (dirName == null)
                return true;

            // Extract the package ID from the directory name (e.g., "System.Text.Json.8.0.5" → "System.Text.Json")
            // so we match the exact package ID rather than any prefix (which would confuse
            // "Microsoft.Extensions.Logging" with "Microsoft.Extensions.Logging.Abstractions").
            var extractedId = ExtractPackageIdFromDirName(dirName);
            if (extractedId == null)
                return true;

            foreach (var package in NuGetConfig.Packages)
            {
                if (string.Equals(extractedId, package.Id, System.StringComparison.OrdinalIgnoreCase))
                    return package.IncludeInBuild;
            }

            // Transitive dependency — include in builds by default.
            return true;
        }

        static string? ExtractPackageNameFromAssetPath(string assetPath)
        {
            var normalized = assetPath.Replace('\\', '/');
            const string prefix = "Packages/";
            if (!normalized.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                return null;

            var start = prefix.Length;
            var slash = normalized.IndexOf('/', start);
            return slash < 0 ? null : normalized.Substring(start, slash - start);
        }

        static bool IsManagedOpenUpmPackageAssetPath(string assetPath)
        {
            var packageName = ExtractPackageNameFromAssetPath(assetPath);
            return packageName != null && NuGetConfig.IsManagedOpenUpmPackageName(packageName);
        }

        /// <summary>
        /// Extracts the package ID from a directory name like "System.Text.Json.8.0.5"
        /// or "Microsoft.AspNetCore.SignalR.Protocols.Json.8.0.15".
        /// </summary>
        static string? ExtractPackageIdFromDirName(string dirName)
        {
            var parts = dirName.Split('.');
            for (var i = 1; i < parts.Length; i++)
            {
                if (parts[i].Length == 0 || !char.IsDigit(parts[i][0]))
                    continue;

                var versionPart = string.Join(".", parts, i, parts.Length - i);
                if (System.Version.TryParse(versionPart, out _))
                    return string.Join(".", parts, 0, i);
            }
            return null;
        }
    }
}

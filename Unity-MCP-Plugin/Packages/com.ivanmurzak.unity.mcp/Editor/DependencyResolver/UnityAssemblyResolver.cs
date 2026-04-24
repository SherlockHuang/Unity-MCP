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
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Detects assemblies that Unity already provides (built-in BCL, engine modules, other packages).
    /// Uses the same approach as NuGetForUnity's UnityPreImportedLibraryResolver:
    ///   1. Get all player assembly references via CompilationPipeline
    ///   2. Skip references that resolve from our NuGet install folder (those are our own duplicates)
    ///   3. Result = assemblies Unity provides — these should NOT be installed from NuGet
    ///
    /// This prevents conflicts like CS1705 (version mismatch) and "multiple assemblies" errors.
    /// </summary>
    static class UnityAssemblyResolver
    {
        static HashSet<string>? cachedUnityAssemblies;

        /// <summary>
        /// Returns true if the given assembly name is already provided by Unity
        /// (either as a BCL assembly or from another installed package).
        /// </summary>
        public static bool IsAlreadyImported(string assemblyName)
        {
            return GetUnityProvidedAssemblies().Contains(assemblyName);
        }

        /// <summary>
        /// Invalidates the cached set of Unity-provided assemblies.
        /// Call this after installing or removing packages.
        /// </summary>
        public static void InvalidateCache()
        {
            cachedUnityAssemblies = null;
        }

        /// <summary>
        /// Gets the set of assembly names that Unity already provides.
        /// Caches the result for performance (invalidated between domain reloads).
        /// </summary>
        public static IReadOnlyCollection<string> GetUnityProvidedAssemblies()
        {
            if (cachedUnityAssemblies != null)
                return cachedUnityAssemblies;

            // Get all assemblies referenced by player builds (includes BCL, engine, other packages)
            var playerAssemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies)
                .Where(a => (a.flags & AssemblyFlags.EditorAssembly) == 0);

            var unityProvided = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            // Canonicalize managed dependency roots to absolute paths once.
            // `assembly.allReferences` paths are typically absolute, while package asset paths
            // are project-relative or virtual. If we do not skip our own bundled/OpenUPM DLLs
            // here, we incorrectly record them as Unity-provided and later disable or exclude
            // them from the editor during PluginImporter configuration.
            var managedDependencyRoots = GetManagedDependencyRoots();

            foreach (var assembly in playerAssemblies)
            {
                foreach (var reference in assembly.allReferences)
                {
                    var name = Path.GetFileNameWithoutExtension(reference);

                    // Resolve references whose path is outside our NuGet install folder
                    // (these are genuinely Unity-provided and would conflict with NuGet duplicates).
                    // If the reference path points into our NuGet install folder we skip it here —
                    // but we still want to detect the *name* as Unity-provided if any other
                    // reference resolves the same name outside our install folder.
                    var fullRef = Path.GetFullPath(reference).Replace('\\', '/');
                    if (managedDependencyRoots.Any(root => fullRef.StartsWith(root, System.StringComparison.OrdinalIgnoreCase)))
                        continue;

                    unityProvided.Add(name);
                }
            }

            // Hard-coded additions (following NuGetForUnity's pattern)
            var compatLevel = PlayerSettings.GetApiCompatibilityLevel(
                UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup));

            if (compatLevel == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                unityProvided.Add("NETStandard.Library");
                unityProvided.Add("Microsoft.NETCore.Platforms");
            }
            unityProvided.Add("Microsoft.CSharp");

            cachedUnityAssemblies = unityProvided;
            return unityProvided;
        }

        static List<string> GetManagedDependencyRoots()
        {
            var roots = new List<string>();

            foreach (var package in PackageInfo.GetAllRegisteredPackages())
            {
                if (!string.Equals(package.name, NuGetConfig.PackageName, System.StringComparison.OrdinalIgnoreCase)
                    && !NuGetConfig.IsManagedOpenUpmPackageName(package.name))
                    continue;

                var root = package.resolvedPath;
                if (string.IsNullOrEmpty(root))
                    continue;

                roots.Add(NormalizeRoot(root));
            }

            // Fallback for local embedded packages where CompilationPipeline may surface
            // project-relative package paths before PackageInfo has a resolved path.
            roots.Add(NormalizeRoot(NuGetConfig.InstallPath));

            return roots;
        }

        static string NormalizeRoot(string path)
        {
            var normalized = Path.GetFullPath(path).Replace('\\', '/');
            return normalized.EndsWith("/") ? normalized : normalized + "/";
        }

    }
}

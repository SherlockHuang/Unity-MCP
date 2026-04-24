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
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Entry point for dependency PluginImporter management. Runs on every domain reload via [InitializeOnLoad].
    ///
    /// This assembly has ZERO external dependencies — it always compiles, even when the main plugin
    /// fails due to missing or conflicting DLLs. It configures Unity-MCP-owned bundled DLLs and sets
    /// the UNITY_MCP_OPENUPM_READY define so the main plugin assemblies can compile. Third-party NuGet DLLs
    /// are resolved through OpenUPM wrapper package dependencies declared in package.json.
    ///
    /// Flow:
    ///   1. [InitializeOnLoad] fires on domain reload
    ///   2. Deferred via EditorApplication.update (runs without editor focus, unlike delayCall)
    ///   3. Configures bundled DLL import settings
    ///   4. Sets UNITY_MCP_OPENUPM_READY scripting define
    ///   5. On next reload: everything is in place, main plugin compiles
    /// </summary>
    [InitializeOnLoad]
    static class NuGetDependencyResolver
    {
        const string Tag = "[Unity-MCP DependencyResolver]";
        const string ReadyDefine = "UNITY_MCP_OPENUPM_READY";
        static bool isResolving;
        static bool isResolved;

        static NuGetDependencyResolver()
        {
            // In short-lived batchmode imports, Unity may quit before the first
            // EditorApplication.update tick. Run synchronously so package DLL importers
            // are configured before gated assemblies are compiled on the next reload.
            if (Application.isBatchMode || IsCi())
            {
                ResolveOnce();
                return;
            }

            EditorApplication.update += ResolveOnce;
        }

        static void ResolveOnce()
        {
            EditorApplication.update -= ResolveOnce;
            ResolveNow();
        }

        internal static void ResolveNow()
        {
            if (isResolving || isResolved)
                return;

            isResolving = true;
            try
            {
                // Configure PluginImporter settings for bundled Unity-MCP-owned DLLs.
                var configuredCount = NuGetPluginConfigurator.ConfigureAll();
                if (configuredCount == 0)
                    throw new InvalidOperationException("No dependency DLL importers were found. Package assets may not be imported yet.");

                EnsureScriptingDefine();
                isResolved = true;
            }
            catch (Exception ex)
            {
                // Do NOT set UNITY_MCP_OPENUPM_READY here: if restore/configuration failed, the DLL
                // layout is unknown/inconsistent, and letting main-plugin assemblies compile
                // against a partial/mismatched set (via defineConstraints) produces hard-to-
                // diagnose MissingMethodException / TypeLoadException at runtime. Surface the
                // failure loud and clear instead, so the user fixes the underlying problem
                // and retries (the next domain reload will run Restore again).
                Debug.LogError($"{Tag} Failed: {ex}");
            }
            finally
            {
                isResolving = false;
            }
        }

        /// <summary>
        /// Checks if the current environment is a CI environment.
        /// Mirrors EnvironmentUtils.IsCi() but without external dependencies,
        /// since this assembly must compile standalone.
        /// Checks both command-line arguments and environment variables for
        /// CI, GITHUB_ACTIONS, and TF_BUILD (Azure Pipelines).
        /// </summary>
        static bool IsCi()
        {
            var args = ParseCommandLineArguments();

            var ci = GetArgOrEnv(args, "CI");
            var gha = GetArgOrEnv(args, "GITHUB_ACTIONS");
            var az = GetArgOrEnv(args, "TF_BUILD");

            return IsTrue(ci) || IsTrue(gha) || IsTrue(az);

            static string? GetArgOrEnv(Dictionary<string, string?> args, string key)
                => args.TryGetValue(key, out var v) ? v : Environment.GetEnvironmentVariable(key);

            static bool IsTrue(string? value)
                => string.Equals(value?.Trim()?.Trim('"'), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses Unity command-line arguments into a dictionary.
        /// Handles both "-key value" and "-key=value" forms, plus bare flags like "-batchmode".
        /// Keys are stored WITHOUT the leading dash.
        /// </summary>
        static Dictionary<string, string?> ParseCommandLineArguments()
        {
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var rawArgs = Environment.GetCommandLineArgs();

            for (var i = 0; i < rawArgs.Length; i++)
            {
                var arg = rawArgs[i];
                if (!arg.StartsWith("-"))
                    continue;

                var key = arg.TrimStart('-');

                // Handle -key=value form
                var eqIndex = key.IndexOf('=');
                if (eqIndex >= 0)
                {
                    result[key.Substring(0, eqIndex)] = key.Substring(eqIndex + 1);
                    continue;
                }

                // Handle -key value form (next arg is value if it doesn't start with -)
                if (i + 1 < rawArgs.Length && !rawArgs[i + 1].StartsWith("-"))
                {
                    result[key] = rawArgs[++i];
                }
                else
                {
                    // Bare flag like -batchmode
                    result[key] = null;
                }
            }

            return result;
        }

        /// <summary>
        /// Ensures the UNITY_MCP_OPENUPM_READY scripting define is set for every supported
        /// build target group. Applying it only to the currently selected group would let
        /// target switching (e.g., Standalone → Android) reintroduce compilation failures
        /// in assemblies gated by defineConstraints.
        /// </summary>
        static void EnsureScriptingDefine()
        {
            var added = new List<string>();

            foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (group == BuildTargetGroup.Unknown)
                    continue;

                NamedBuildTarget target;
                try
                {
                    target = NamedBuildTarget.FromBuildTargetGroup(group);
                }
                catch
                {
                    continue;
                }

                if (TryAddDefine(target))
                    added.Add(target.TargetName);
            }

            // Server is a distinct NamedBuildTarget not reachable via BuildTargetGroup.
            if (TryAddDefine(NamedBuildTarget.Server))
                added.Add(NamedBuildTarget.Server.TargetName);

            if (added.Count > 0)
                Debug.Log($"{Tag} Added '{ReadyDefine}' scripting define for: {string.Join(", ", added)}.");
        }

        static bool TryAddDefine(NamedBuildTarget target)
        {
            try
            {
                PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
                if (defines.Contains(ReadyDefine))
                    return false;

                var newDefines = new string[defines.Length + 1];
                Array.Copy(defines, newDefines, defines.Length);
                newDefines[defines.Length] = ReadyDefine;

                PlayerSettings.SetScriptingDefineSymbols(target, newDefines);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    sealed class NuGetDependencyAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!HasDependencyAsset(importedAssets) && !HasDependencyAsset(movedAssets))
                return;

            NuGetDependencyResolver.ResolveNow();
        }

        static bool HasDependencyAsset(string[] assetPaths)
        {
            foreach (var assetPath in assetPaths)
            {
                var normalized = assetPath.Replace('\\', '/');
                if (!normalized.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (normalized.StartsWith(NuGetConfig.InstallPath, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (normalized.StartsWith("Packages/" + NuGetConfig.OpenUpmNuGetPackagePrefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}

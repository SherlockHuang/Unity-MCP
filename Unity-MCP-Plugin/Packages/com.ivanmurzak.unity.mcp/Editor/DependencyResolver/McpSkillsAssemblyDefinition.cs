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
using System.IO;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Keeps project-local generated MCP skill tools in an asmdef that can reference
    /// Unity-MCP's explicitly referenced precompiled DLLs.
    /// </summary>
    public static class McpSkillsAssemblyDefinition
    {
        const string Tag = NuGetConfig.LogTag;
        const string SkillsFolder = "Assets/Editor/McpSkills";
        const string AsmdefPath = SkillsFolder + "/McpSkills.asmdef";

        const string AsmdefContent =
@"{
    ""name"": ""com.IvanMurzak.Unity.MCP.GeneratedSkills"",
    ""rootNamespace"": ""com.IvanMurzak.Unity.MCP.Editor.API"",
    ""references"": [
        ""com.IvanMurzak.Unity.MCP.Editor"",
        ""com.IvanMurzak.Unity.MCP.Runtime""
    ],
    ""includePlatforms"": [
        ""Editor""
    ],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": true,
    ""precompiledReferences"": [
        ""McpPlugin.dll"",
        ""McpPlugin.Common.dll"",
        ""ReflectorNet.dll""
    ],
    ""autoReferenced"": true,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}
";

        public static void EnsureForExistingSkills()
        {
            var absoluteSkillsFolder = AbsoluteSkillsFolder;
            if (!Directory.Exists(absoluteSkillsFolder))
                return;

            if (Directory.GetFiles(absoluteSkillsFolder, "*.cs", SearchOption.AllDirectories).Length == 0)
                return;

            Ensure();
        }

        public static void EnsureForScriptPath(string path)
        {
            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith(SkillsFolder + "/", StringComparison.OrdinalIgnoreCase))
                return;

            Ensure();
        }

        static void Ensure()
        {
            if (HasUserAsmdef())
                return;

            var absoluteSkillsFolder = AbsoluteSkillsFolder;
            var absoluteAsmdefPath = AbsoluteAsmdefPath;

            Directory.CreateDirectory(absoluteSkillsFolder);

            if (File.Exists(absoluteAsmdefPath) && File.ReadAllText(absoluteAsmdefPath) == AsmdefContent)
                return;

            File.WriteAllText(absoluteAsmdefPath, AsmdefContent);
            AssetDatabase.ImportAsset(AsmdefPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"{Tag} Created generated skills assembly definition: {AsmdefPath}");
        }

        static bool HasUserAsmdef()
        {
            var absoluteSkillsFolder = AbsoluteSkillsFolder;
            if (!Directory.Exists(absoluteSkillsFolder))
                return false;

            foreach (var asmdefPath in Directory.GetFiles(absoluteSkillsFolder, "*.asmdef", SearchOption.TopDirectoryOnly))
            {
                var normalized = ToAssetPath(asmdefPath);
                if (!string.Equals(normalized, AsmdefPath, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static string AbsoluteSkillsFolder
            => Path.Combine(ProjectRootPath, SkillsFolder.Replace('/', Path.DirectorySeparatorChar));

        static string AbsoluteAsmdefPath
            => Path.Combine(ProjectRootPath, AsmdefPath.Replace('/', Path.DirectorySeparatorChar));

        static string ProjectRootPath
            => Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();

        static string ToAssetPath(string absolutePath)
        {
            var projectRoot = ProjectRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var relativePath = absolutePath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return relativePath.Replace('\\', '/');
        }
    }
}

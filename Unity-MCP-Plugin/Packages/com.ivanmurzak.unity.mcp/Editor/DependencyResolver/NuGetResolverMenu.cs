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
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DependencyResolver
{
    /// <summary>
    /// Adds a Unity Editor menu item that reapplies bundled dependency DLL importer settings.
    /// </summary>
    static class NuGetResolverMenu
    {
        const string Tag = NuGetConfig.LogTag;
        const string MenuPath = "Tools/AI Game Developer/Dependencies/Reconfigure Dependency DLLs";

        [MenuItem(MenuPath, priority = 1050)]
        public static void ForceResolve()
        {
            Debug.Log($"{Tag} Dependency DLL reconfigure requested...");

            try
            {
                var configuredCount = NuGetPluginConfigurator.ConfigureAll();
                if (configuredCount == 0)
                    throw new InvalidOperationException("Required dependency DLL importers were not found or were not configured.");

                Debug.Log($"{Tag} Dependency DLL reconfigure complete. Configured {configuredCount} DLL importer(s).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} Dependency DLL reconfigure failed: {ex}");
            }
        }
    }
}

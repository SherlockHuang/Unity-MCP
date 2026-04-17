/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections;
using System.Linq;
using System.Text.Json;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolCatalogMinimizationTests : BaseTest
    {
        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
            yield return null;
            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            yield return null;
        }

        [UnityTest]
        public IEnumerator LocalToolRegistry_PreservesRichInputSchemaDescriptions()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            var tool = toolManager!.GetAllTools().FirstOrDefault(x => x.Name == Tool_Tool.ToolSetEnabledStateId);
            Assert.IsNotNull(tool, $"{Tool_Tool.ToolSetEnabledStateId} should be present in the live registry");
            Assert.IsNotNull(tool!.InputSchema, "Live registry should retain the full input schema");

            var schemaJson = tool.InputSchema!.ToJsonString();
            StringAssert.Contains("\"description\"", schemaJson, "Local/editor consumers should still see rich schema descriptions");
            StringAssert.Contains("desired enabled state", schemaJson, "Local/editor consumers should retain nested schema guidance");
        }

        [UnityTest]
        public IEnumerator ToolListHelper_StillUsesFullSchemaForRichInputDescriptions()
        {
            yield return null;

            var json = RunTool(Tool_Tool.ToolListId, @"{
                ""regexSearch"": ""^tool-set-enabled-state$"",
                ""includeInputs"": ""InputsWithDescription""
            }").Value!.GetMessage()!;

            using var doc = JsonDocument.Parse(json);
            var result = doc.RootElement.TryGetProperty("result", out var resultEl)
                ? resultEl
                : doc.RootElement;

            Assert.AreEqual(1, result.GetArrayLength(), "tool-list helper should return one exact match");

            var inputs = result[0].GetProperty("inputs");
            Assert.Greater(inputs.GetArrayLength(), 0, "tool-list helper should still parse inputs");

            var toolsInput = inputs.EnumerateArray().FirstOrDefault(x => x.GetProperty("name").GetString() == "tools");
            Assert.AreEqual(JsonValueKind.Object, toolsInput.ValueKind, "tools input should be present");
            Assert.IsTrue(toolsInput.TryGetProperty("description", out var descriptionNode), "Full-schema helper path should still include descriptions");
            StringAssert.Contains("desired enabled state", descriptionNode.GetString());
        }
    }
}

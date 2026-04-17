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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.API;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    [TestFixture]
    public class ToolGetDetailTests : BaseTest
    {
        bool? _originalToolGetDetailEnabled;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();

            UnityMcpPluginEditor.Instance.DisposeMcpPluginInstance();
            yield return null;
            UnityMcpPluginEditor.Instance.BuildMcpPluginIfNeeded();
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            Assert.IsNotNull(toolManager, "ToolManager should not be null");

            _originalToolGetDetailEnabled = toolManager!.IsToolEnabled(Tool_Tool.ToolGetDetailId);
            toolManager.SetToolEnabled(Tool_Tool.ToolGetDetailId, true);
            UnityMcpPluginEditor.Instance.Save();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            var toolManager = UnityMcpPluginEditor.Instance.Tools;
            if (toolManager != null && _originalToolGetDetailEnabled.HasValue)
            {
                toolManager.SetToolEnabled(Tool_Tool.ToolGetDetailId, _originalToolGetDetailEnabled.Value);
                UnityMcpPluginEditor.Instance.Save();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        public IEnumerator GetDetail_ExactLookup_ReturnsStructuredSuccessWithoutSchemasByDefault()
        {
            yield return null;

            var root = RunGetDetail(@"{
                ""name"": ""tool-list""
            }");

            Assert.IsTrue(root.GetProperty("success").GetBoolean(), "Lookup should succeed");
            Assert.AreEqual("tool-list", root.GetProperty("resolvedName").GetString());
            Assert.AreEqual("tool-list", root.GetProperty("name").GetString());

            Assert.IsTrue(root.TryGetProperty("inputs", out var inputs), "Parsed arguments should be included by default");
            Assert.Greater(inputs.GetArrayLength(), 0, "Parsed arguments should include tool-list inputs");

            Assert.IsTrue(!root.TryGetProperty("inputSchema", out var inputSchema) || inputSchema.ValueKind == JsonValueKind.Null,
                "Full input schema should be omitted by default");
            Assert.IsTrue(!root.TryGetProperty("outputSchema", out var outputSchema) || outputSchema.ValueKind == JsonValueKind.Null,
                "Full output schema should be omitted by default");
        }

        [UnityTest]
        public IEnumerator GetDetail_IncludeSchemas_ReturnsRichSchemaPayload()
        {
            yield return null;

            var root = RunGetDetail(@"{
                ""name"": ""tool-set-enabled-state"",
                ""includeSchemas"": true
            }");

            Assert.IsTrue(root.GetProperty("success").GetBoolean(), "Lookup should succeed");
            Assert.IsTrue(root.TryGetProperty("inputSchema", out var inputSchema), "Full input schema should be included when requested");
            Assert.AreEqual(JsonValueKind.Object, inputSchema.ValueKind);
            StringAssert.Contains("\"description\"", inputSchema.GetRawText(), "On-demand detail path should preserve rich schema descriptions");
        }

        [UnityTest]
        public IEnumerator GetDetail_UnknownTool_ReturnsStructuredNotFoundFailure()
        {
            yield return null;

            var root = RunGetDetail(@"{
                ""name"": ""definitely-not-a-real-tool""
            }");

            Assert.IsFalse(root.GetProperty("success").GetBoolean(), "Lookup should fail");
            var failure = root.GetProperty("failure");
            Assert.AreEqual("not-found", failure.GetProperty("code").GetString());
            StringAssert.Contains("definitely-not-a-real-tool", failure.GetProperty("message").GetString());
        }

        [UnityTest]
        public IEnumerator GetDetail_EmptyName_ReturnsStructuredEmptyNameFailure()
        {
            yield return null;

            var root = RunGetDetail(@"{
                ""name"": ""   ""
            }");

            Assert.IsFalse(root.GetProperty("success").GetBoolean(), "Lookup should fail");
            var failure = root.GetProperty("failure");
            Assert.AreEqual("empty-name", failure.GetProperty("code").GetString());
        }

        [UnityTest]
        public IEnumerator GetDetail_AmbiguousCaseInsensitiveLookup_ReturnsStructuredMatches()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            const string firstName = "Batch-One-Ambiguous";
            const string secondName = "batch-one-ambiguous";
            const string lookupName = "bAtCh-OnE-AmBiGuOuS";

            toolManager.AddTool(firstName, new TestRunToolStub(firstName));
            toolManager.AddTool(secondName, new TestRunToolStub(secondName));

            try
            {
                var root = RunGetDetail($@"{{
                    ""name"": ""{lookupName}""
                }}");

                Assert.IsFalse(root.GetProperty("success").GetBoolean(), "Lookup should fail when case-insensitive name is ambiguous");
                var failure = root.GetProperty("failure");
                Assert.AreEqual("ambiguous", failure.GetProperty("code").GetString());

                var matches = failure.GetProperty("matches").EnumerateArray().Select(x => x.GetString()).ToArray();
                CollectionAssert.AreEquivalent(new[] { firstName, secondName }, matches);
            }
            finally
            {
                toolManager.RemoveTool(firstName);
                toolManager.RemoveTool(secondName);
            }
        }

        [Test]
        public void CreateInternalErrorResult_SanitizesClientFacingMessage()
        {
            LogAssert.Expect(LogType.Error, new Regex("tool-get-detail failed for 'tool-list': System\\.InvalidOperationException: sensitive details"));
            var result = Tool_Tool.CreateInternalErrorResult("tool-list", new InvalidOperationException("sensitive details"));

            Assert.IsFalse(result.Success, "Internal failures should return a structured failure result");
            Assert.IsNotNull(result.Failure, "Internal failures should include failure details");
            Assert.AreEqual("internal-error", result.Failure!.Code);
            Assert.AreEqual(Tool_Tool.ToolGetDetailInternalErrorMessage, result.Failure.Message);
            Assert.IsFalse(result.Failure.Message.Contains("sensitive details"), "Client-facing message should be sanitized");
        }

        JsonElement RunGetDetail(string inputJson)
        {
            var json = RunTool(Tool_Tool.ToolGetDetailId, inputJson).Value!.GetMessage()!;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.TryGetProperty("result", out var resultEl)
                ? resultEl.Clone()
                : doc.RootElement.Clone();
            return root;
        }

        sealed class TestRunToolStub : IRunTool
        {
            readonly JsonNode _inputSchema = JsonNode.Parse(@"{""type"":""object"",""properties"":{""value"":{""type"":""string"",""description"":""stub value""}}}")!;

            public TestRunToolStub(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string Title => Name;
            public string Description => "Test-only stub tool";
            public JsonNode? InputSchema => _inputSchema;
            public JsonNode? OutputSchema => null;
            public int TokenCount => 1;
            public McpToolType ToolType => default;
            public bool Enabled { get; set; } = true;
            public bool? ReadOnlyHint => true;
            public bool? IdempotentHint => true;
            public bool? DestructiveHint => false;
            public bool? OpenWorldHint => false;

            public Task<ResponseCallTool> Run(
                string requestId,
                IReadOnlyDictionary<string, JsonElement>? namedParameters,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new ResponseCallTool(requestId, new JsonObject
                {
                    ["ok"] = true
                }, ResponseStatus.Success));
            }
        }
    }
}

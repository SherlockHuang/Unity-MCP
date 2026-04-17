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
using System.Text.Json.Serialization;
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
        public IEnumerator GetDetail_DefaultRequest_ReturnsCompactSummaryFieldMatrix()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-list");

            AssertSuccessFieldMatrix(root, requestedName: "tool-list", resolvedName: "tool-list", expectInputs: true, expectedEnabled: GetExpectedEnabled("tool-list"));
            AssertSchemaOmitted(root, "inputSchema");
            AssertSchemaOmitted(root, "outputSchema");
        }

        [UnityTest]
        public IEnumerator GetDetail_ExplicitFull_ReturnsSummaryFieldsPlusSchemas()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "full");

            AssertSuccessFieldMatrix(root, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: true, expectedEnabled: GetExpectedEnabled("tool-set-enabled-state"));
            AssertSchemaObject(root, "inputSchema");
            AssertSchemaObject(root, "outputSchema");

            var inputSchema = root.GetProperty("inputSchema");
            StringAssert.Contains("\"description\"", inputSchema.GetRawText(), "On-demand detail path should preserve rich schema descriptions");
        }

        [UnityTest]
        public IEnumerator GetDetail_SummaryAndFull_ShareCompatibleSuccessShape()
        {
            yield return null;

            var summary = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "summary");
            var full = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "full");

            var expectedEnabled = GetExpectedEnabled("tool-set-enabled-state");

            AssertSuccessFieldMatrix(summary, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: true, expectedEnabled: expectedEnabled);
            AssertSuccessFieldMatrix(full, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: true, expectedEnabled: expectedEnabled);

            CollectionAssert.AreEquivalent(GetSharedSuccessPropertyNames(summary), GetSharedSuccessPropertyNames(full),
                "Summary and full should expose the same shared success properties");

            foreach (var propertyName in SharedSuccessFieldNames)
            {
                AssertJsonValuesEqual(summary.GetProperty(propertyName), full.GetProperty(propertyName), propertyName);
            }

            AssertSchemaOmitted(summary, "inputSchema");
            AssertSchemaOmitted(summary, "outputSchema");
            AssertSchemaObject(full, "inputSchema");
            AssertSchemaObject(full, "outputSchema");
        }

        [UnityTest]
        public IEnumerator GetDetail_LegacyIncludeSchemas_True_MapsToFullDetail()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-set-enabled-state", includeSchemas: true);

            AssertSuccessFieldMatrix(root, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: true, expectedEnabled: GetExpectedEnabled("tool-set-enabled-state"));
            AssertSchemaObject(root, "inputSchema");
            AssertSchemaObject(root, "outputSchema");
        }

        [UnityTest]
        public IEnumerator GetDetail_ExplicitFull_WinsOverLegacyIncludeSchemasFalse()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "full", includeSchemas: false);

            AssertSuccessFieldMatrix(root, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: true, expectedEnabled: GetExpectedEnabled("tool-set-enabled-state"));
            AssertSchemaObject(root, "inputSchema");
            AssertSchemaObject(root, "outputSchema");
        }

        [UnityTest]
        public IEnumerator GetDetail_IncludeParsedArgumentsFalse_SuppressesInputsInSummaryAndFull()
        {
            yield return null;

            var summary = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "summary", includeParsedArguments: false);
            var full = RunGetDetail(name: "tool-set-enabled-state", detailLevel: "full", includeParsedArguments: false);

            var expectedEnabled = GetExpectedEnabled("tool-set-enabled-state");

            AssertSuccessFieldMatrix(summary, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: false, expectedEnabled: expectedEnabled);
            AssertSuccessFieldMatrix(full, requestedName: "tool-set-enabled-state", resolvedName: "tool-set-enabled-state", expectInputs: false, expectedEnabled: expectedEnabled);

            AssertSchemaOmitted(summary, "inputSchema");
            AssertSchemaOmitted(summary, "outputSchema");
            AssertSchemaObject(full, "inputSchema");
            AssertSchemaObject(full, "outputSchema");
        }

        [UnityTest]
        public IEnumerator GetDetail_SummaryWithoutParsedArguments_SucceedsWithoutTouchingInputSchema()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            const string toolName = "batch-one-throwing-schema-no-inputs";

            toolManager.AddTool(toolName, new ThrowingSchemaToolStub(toolName));

            try
            {
                var root = RunGetDetail(name: toolName, detailLevel: "summary", includeParsedArguments: false);

                AssertSuccessFieldMatrix(root, requestedName: toolName, resolvedName: toolName, expectInputs: false, expectedEnabled: true);
                AssertSchemaOmitted(root, "inputSchema");
                AssertSchemaOmitted(root, "outputSchema");
            }
            finally
            {
                toolManager.RemoveTool(toolName);
            }
        }

        [UnityTest]
        public IEnumerator GetDetail_UnknownTool_ReturnsStructuredNotFoundFailure()
        {
            yield return null;

            var root = RunGetDetail(name: "definitely-not-a-real-tool");

            AssertFailureFieldMatrix(root, requestedName: "definitely-not-a-real-tool", expectedCode: "not-found");
            StringAssert.Contains("definitely-not-a-real-tool", root.GetProperty("failure").GetProperty("message").GetString());
        }

        [UnityTest]
        public IEnumerator GetDetail_UnknownTool_FullMode_PreservesStructuredFailureShape()
        {
            yield return null;

            var root = RunGetDetail(name: "definitely-not-a-real-tool", detailLevel: "full");

            AssertFailureFieldMatrix(root, requestedName: "definitely-not-a-real-tool", expectedCode: "not-found");
        }

        [UnityTest]
        public IEnumerator GetDetail_EmptyName_ReturnsStructuredEmptyNameFailure()
        {
            yield return null;

            var root = RunGetDetail(name: "   ");

            AssertFailureFieldMatrix(root, requestedName: "   ", expectedCode: "empty-name");
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
                var root = RunGetDetail(name: lookupName);

                AssertFailureFieldMatrix(root, requestedName: lookupName, expectedCode: "ambiguous");
                var failure = root.GetProperty("failure");

                var matches = failure.GetProperty("matches").EnumerateArray().Select(x => x.GetString()).ToArray();
                CollectionAssert.AreEquivalent(new[] { firstName, secondName }, matches);
            }
            finally
            {
                toolManager.RemoveTool(firstName);
                toolManager.RemoveTool(secondName);
            }
        }

        [UnityTest]
        public IEnumerator GetDetail_InvalidDetailLevel_ReturnsStructuredValidationFailure()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-list", detailLevel: "verbose");

            AssertFailureFieldMatrix(root, requestedName: "tool-list", expectedCode: "invalid-detail-level");
            StringAssert.Contains("summary", root.GetProperty("failure").GetProperty("message").GetString());
            StringAssert.Contains("full", root.GetProperty("failure").GetProperty("message").GetString());
        }

        [UnityTest]
        public IEnumerator GetDetail_InvalidDetailLevel_TakesPrecedenceOverLegacyFlagConflict()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-list", detailLevel: "verbose", includeSchemas: true);

            AssertFailureFieldMatrix(root, requestedName: "tool-list", expectedCode: "invalid-detail-level");
        }

        [UnityTest]
        public IEnumerator GetDetail_SummaryDetailLevel_WithLegacyIncludeSchemasTrue_ReturnsConflictFailure()
        {
            yield return null;

            var root = RunGetDetail(name: "tool-list", detailLevel: "summary", includeSchemas: true);

            AssertFailureFieldMatrix(root, requestedName: "tool-list", expectedCode: "conflicting-detail-request");
            StringAssert.Contains("includeSchemas", root.GetProperty("failure").GetProperty("message").GetString());
        }

        [UnityTest]
        public IEnumerator GetDetail_SummaryPath_InternalFailure_ReturnsStructuredInternalError()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            const string toolName = "batch-one-throwing-schema";

            toolManager.AddTool(toolName, new ThrowingSchemaToolStub(toolName));

            try
            {
                LogAssert.Expect(LogType.Error, new Regex("tool-get-detail failed for 'batch-one-throwing-schema': System\\.InvalidOperationException: sensitive details"));

                var root = RunGetDetail(name: toolName, detailLevel: "summary");

                AssertFailureFieldMatrix(root, requestedName: toolName, expectedCode: "internal-error");
                Assert.AreEqual(Tool_Tool.ToolGetDetailInternalErrorMessage, root.GetProperty("failure").GetProperty("message").GetString());
                Assert.IsFalse(root.GetProperty("failure").GetProperty("message").GetString()!.Contains("sensitive details"),
                    "Client-facing message should stay sanitized on endpoint-level internal failures");
            }
            finally
            {
                toolManager.RemoveTool(toolName);
            }
        }

        [UnityTest]
        public IEnumerator GetDetail_FullPath_OutputSchemaFailure_ReturnsStructuredInternalError()
        {
            yield return null;

            var toolManager = UnityMcpPluginEditor.Instance.Tools!;
            const string toolName = "batch-one-throwing-output-schema";

            toolManager.AddTool(toolName, new ThrowingOutputSchemaToolStub(toolName));

            try
            {
                var summary = RunGetDetail(name: toolName, detailLevel: "summary");

                AssertSuccessFieldMatrix(summary, requestedName: toolName, resolvedName: toolName, expectInputs: true, expectedEnabled: true);
                AssertSchemaOmitted(summary, "inputSchema");
                AssertSchemaOmitted(summary, "outputSchema");

                LogAssert.Expect(LogType.Error, new Regex("tool-get-detail failed for 'batch-one-throwing-output-schema': System\\.InvalidOperationException: sensitive full-mode details"));

                var root = RunGetDetail(name: toolName, detailLevel: "full");

                AssertFailureFieldMatrix(root, requestedName: toolName, expectedCode: "internal-error");
                Assert.AreEqual(Tool_Tool.ToolGetDetailInternalErrorMessage, root.GetProperty("failure").GetProperty("message").GetString());
                Assert.IsFalse(root.GetProperty("failure").GetProperty("message").GetString()!.Contains("sensitive full-mode details"),
                    "Client-facing message should stay sanitized on endpoint-level internal failures");
            }
            finally
            {
                toolManager.RemoveTool(toolName);
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

        static readonly string[] SharedSuccessFieldNames =
        {
            "success",
            "requestedName",
            "resolvedName",
            "name",
            "title",
            "description",
            "enabled",
            "tokenCount",
            "readOnlyHint",
            "idempotentHint",
            "destructiveHint",
            "openWorldHint",
            "inputs"
        };

        static readonly string[] FailureOnlyFieldNames =
        {
            "resolvedName",
            "name",
            "title",
            "description",
            "enabled",
            "tokenCount",
            "readOnlyHint",
            "idempotentHint",
            "destructiveHint",
            "openWorldHint",
            "inputs",
            "inputSchema",
            "outputSchema"
        };

        JsonElement RunGetDetail(
            string? name = null,
            string? detailLevel = null,
            bool? includeSchemas = null,
            bool? includeParsedArguments = null)
        {
            var inputJson = SerializeInput(new ToolGetDetailRequest
            {
                Name = name,
                DetailLevel = detailLevel,
                IncludeSchemas = includeSchemas,
                IncludeParsedArguments = includeParsedArguments
            });

            var json = RunTool(Tool_Tool.ToolGetDetailId, inputJson).Value!.GetMessage()!;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.TryGetProperty("result", out var resultEl)
                ? resultEl.Clone()
                : doc.RootElement.Clone();
            return root;
        }

        static void AssertSuccessFieldMatrix(JsonElement root, string requestedName, string resolvedName, bool expectInputs, bool? expectedEnabled = null)
        {
            Assert.IsTrue(root.GetProperty("success").GetBoolean(), "Lookup should succeed");
            Assert.AreEqual(requestedName, root.GetProperty("requestedName").GetString());
            Assert.AreEqual(resolvedName, root.GetProperty("resolvedName").GetString());
            Assert.AreEqual(resolvedName, root.GetProperty("name").GetString());
            Assert.AreEqual(JsonValueKind.String, root.GetProperty("title").ValueKind, "Title should be present");
            Assert.AreEqual(JsonValueKind.String, root.GetProperty("description").ValueKind, "Description should be present");
            var enabled = root.GetProperty("enabled");
            Assert.IsTrue(enabled.ValueKind == JsonValueKind.True || enabled.ValueKind == JsonValueKind.False, "Enabled should be boolean");
            if (expectedEnabled.HasValue)
                Assert.AreEqual(expectedEnabled.Value, enabled.GetBoolean(), "Enabled should reflect the tool manager state");
            Assert.AreEqual(JsonValueKind.Number, root.GetProperty("tokenCount").ValueKind, "Token count should be present");
            AssertExecutionHints(root);

            if (expectInputs)
            {
                Assert.IsTrue(root.TryGetProperty("inputs", out var inputs), "Parsed inputs should be included");
                Assert.AreEqual(JsonValueKind.Array, inputs.ValueKind);
            }
            else
            {
                AssertSchemaOmitted(root, "inputs");
            }

            Assert.IsFalse(root.TryGetProperty("failure", out var failure) && failure.ValueKind != JsonValueKind.Null,
                "Successful responses should not include failure details");
        }

        static void AssertFailureFieldMatrix(JsonElement root, string requestedName, string expectedCode)
        {
            Assert.IsFalse(root.GetProperty("success").GetBoolean(), "Lookup should fail");
            Assert.AreEqual(requestedName, root.GetProperty("requestedName").GetString());
            Assert.IsTrue(root.TryGetProperty("failure", out var failure), "Failure response should include failure details");
            Assert.AreEqual(expectedCode, failure.GetProperty("code").GetString());

            foreach (var propertyName in FailureOnlyFieldNames)
            {
                AssertSchemaOmitted(root, propertyName);
            }
        }

        static void AssertExecutionHints(JsonElement root)
        {
            Assert.IsTrue(root.TryGetProperty("readOnlyHint", out var readOnlyHint), "readOnlyHint should be present");
            Assert.IsTrue(root.TryGetProperty("idempotentHint", out var idempotentHint), "idempotentHint should be present");
            Assert.IsTrue(root.TryGetProperty("destructiveHint", out var destructiveHint), "destructiveHint should be present");
            Assert.IsTrue(root.TryGetProperty("openWorldHint", out var openWorldHint), "openWorldHint should be present");

            Assert.IsTrue(IsBooleanOrNull(readOnlyHint), "readOnlyHint should be boolean or null");
            Assert.IsTrue(IsBooleanOrNull(idempotentHint), "idempotentHint should be boolean or null");
            Assert.IsTrue(IsBooleanOrNull(destructiveHint), "destructiveHint should be boolean or null");
            Assert.IsTrue(IsBooleanOrNull(openWorldHint), "openWorldHint should be boolean or null");
        }

        static bool IsBooleanOrNull(JsonElement element)
        {
            return element.ValueKind == JsonValueKind.True ||
                   element.ValueKind == JsonValueKind.False ||
                   element.ValueKind == JsonValueKind.Null;
        }

        static bool GetExpectedEnabled(string toolName)
        {
            return UnityMcpPluginEditor.Instance.Tools!.IsToolEnabled(toolName);
        }

        static void AssertSchemaObject(JsonElement root, string propertyName)
        {
            Assert.IsTrue(root.TryGetProperty(propertyName, out var property), $"{propertyName} should be included");
            Assert.AreEqual(JsonValueKind.Object, property.ValueKind, $"{propertyName} should be an object when included");
        }

        static void AssertSchemaOmitted(JsonElement root, string propertyName)
        {
            Assert.IsTrue(!root.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null,
                $"{propertyName} should be omitted or null");
        }

        static string[] GetSharedSuccessPropertyNames(JsonElement root)
        {
            return root.EnumerateObject()
                .Where(property => property.Name != "inputSchema" && property.Name != "outputSchema")
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }

        static void AssertJsonValuesEqual(JsonElement left, JsonElement right, string propertyName)
        {
            Assert.AreEqual(left.ValueKind, right.ValueKind, $"{propertyName} should have the same JSON kind");
            Assert.AreEqual(left.GetRawText(), right.GetRawText(), $"{propertyName} should have the same JSON value");
        }

        static string SerializeInput(ToolGetDetailRequest request)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            return JsonSerializer.Serialize(request, options);
        }

        sealed class ToolGetDetailRequest
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("detailLevel")]
            public string? DetailLevel { get; set; }

            [JsonPropertyName("includeSchemas")]
            public bool? IncludeSchemas { get; set; }

            [JsonPropertyName("includeParsedArguments")]
            public bool? IncludeParsedArguments { get; set; }
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

        sealed class ThrowingSchemaToolStub : IRunTool
        {
            public ThrowingSchemaToolStub(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string Title => Name;
            public string Description => "Test-only tool whose schema access throws";
            public JsonNode? InputSchema => throw new InvalidOperationException("sensitive details");
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

        sealed class ThrowingOutputSchemaToolStub : IRunTool
        {
            readonly JsonNode _inputSchema = JsonNode.Parse(@"{""type"":""object"",""properties"":{""value"":{""type"":""string"",""description"":""stub value""}}}")!;

            public ThrowingOutputSchemaToolStub(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public string Title => Name;
            public string Description => "Test-only tool whose output schema access throws";
            public JsonNode? InputSchema => _inputSchema;
            public JsonNode? OutputSchema => throw new InvalidOperationException("sensitive full-mode details");
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

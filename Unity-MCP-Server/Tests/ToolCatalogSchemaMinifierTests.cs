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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using NUnit.Framework;

namespace com.IvanMurzak.Unity.MCP.Server.Tests
{
    [TestFixture]
    public class ToolCatalogSchemaMinifierTests
    {
        [Test]
        public void Minimize_StripsSchemaDescriptionsButPreservesStructure()
        {
            var original = CreateTool();
            var result = ToolCatalogSchemaMinifier.Minimize(new ListToolsResult
            {
                Tools = [original]
            });

            Assert.That(result.Tools, Has.Count.EqualTo(1));

            var minimizedTool = result.Tools[0];
            var schemaJson = minimizedTool.InputSchema.GetRawText();
            Assert.That(schemaJson, Does.Not.Contain("\"description\""));

            using var doc = JsonDocument.Parse(schemaJson);
            var root = doc.RootElement;
            Assert.That(root.GetProperty("type").GetString(), Is.EqualTo("object"));
            Assert.That(root.GetProperty("properties").TryGetProperty("tools", out var toolsProperty), Is.True);
            Assert.That(toolsProperty.GetProperty("$ref").GetString(), Is.EqualTo("#/$defs/inputDataArray"));

            var required = root.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray();
            Assert.That(required, Does.Contain("tools"));

            var nestedDefinition = root.GetProperty("$defs").GetProperty("inputData");
            Assert.That(nestedDefinition.GetProperty("properties").TryGetProperty("Name", out var nameProperty), Is.True);
            Assert.That(nameProperty.GetProperty("type").GetString(), Is.EqualTo("string"));
        }

        [Test]
        public void Minimize_DoesNotMutateOriginalToolSchemaOrMetadata()
        {
            var original = CreateTool();
            var originalSchemaJson = original.InputSchema.GetRawText();

            var minimized = ToolCatalogSchemaMinifier.Minimize(new ListToolsResult
            {
                Tools = [original]
            }).Tools[0];

            Assert.That(original.InputSchema.GetRawText(), Is.EqualTo(originalSchemaJson));
            Assert.That(originalSchemaJson, Does.Contain("\"description\""));
            Assert.That(minimized.Description, Is.EqualTo(original.Description));
            Assert.That(minimized.Title, Is.EqualTo(original.Title));
            Assert.That(minimized.Name, Is.EqualTo(original.Name));
        }

        [Test]
        public void Minimize_PreservesEnvelopeMetadata()
        {
            var result = new ListToolsResult
            {
                NextCursor = "cursor-123",
                Meta = new JsonObject
                {
                    ["traceId"] = "trace-456"
                },
                Tools = [CreateTool()]
            };

            var minimized = ToolCatalogSchemaMinifier.Minimize(result);

            Assert.That(minimized.NextCursor, Is.EqualTo("cursor-123"));
            Assert.That(minimized.Meta, Is.Not.Null);
            Assert.That(minimized.Meta!["traceId"]?.ToString(), Is.EqualTo("trace-456"));
            Assert.That(minimized.Meta, Is.Not.SameAs(result.Meta), "Envelope metadata should be preserved without sharing mutable state");
            Assert.That(minimized.Tools, Has.Count.EqualTo(1));
            Assert.That(minimized.Tools[0].InputSchema.GetRawText(), Does.Not.Contain("\"description\""));
        }

        [Test]
        public void Minimize_PreservesRealPropertyNamedDescription()
        {
            using var schemaDoc = JsonDocument.Parse("""
                {
                  "type": "object",
                  "description": "Tool description annotation",
                  "properties": {
                    "description": {
                      "type": "string",
                      "description": "Field guidance that should be stripped"
                    },
                    "count": {
                      "type": "integer",
                      "description": "Counter guidance"
                    }
                  },
                  "required": ["description"]
                }
                """);

            var minimized = ToolCatalogSchemaMinifier.Minimize(new ListToolsResult
            {
                Tools =
                [
                    new Tool
                    {
                        Name = "describe-tool",
                        InputSchema = schemaDoc.RootElement.Clone()
                    }
                ]
            });

            using var minimizedDoc = JsonDocument.Parse(minimized.Tools[0].InputSchema.GetRawText());
            var root = minimizedDoc.RootElement;
            Assert.That(root.TryGetProperty("description", out _), Is.False, "Schema annotations should be stripped");
            Assert.That(root.GetProperty("properties").TryGetProperty("description", out var descriptionField), Is.True,
                "A real input field named 'description' must be preserved");
            Assert.That(descriptionField.GetProperty("type").GetString(), Is.EqualTo("string"));
            Assert.That(descriptionField.TryGetProperty("description", out _), Is.False,
                "Nested field annotations should still be stripped");
            Assert.That(root.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToArray(), Does.Contain("description"));
        }

        static Tool CreateTool()
        {
            using var schemaDoc = JsonDocument.Parse("""
                {
                  "type": "object",
                  "description": "Top-level description",
                  "properties": {
                    "tools": {
                      "$ref": "#/$defs/inputDataArray",
                      "description": "Top-level property description"
                    }
                  },
                  "required": ["tools"],
                  "$defs": {
                    "inputDataArray": {
                      "type": "array",
                      "description": "Array description",
                      "items": {
                        "$ref": "#/$defs/inputData"
                      }
                    },
                    "inputData": {
                      "type": "object",
                      "description": "Nested object description",
                      "properties": {
                        "Name": {
                          "type": "string",
                          "description": "Name description"
                        }
                      },
                      "required": ["Name"]
                    }
                  }
                }
                """);

            return new Tool
            {
                Name = "tool-set-enabled-state",
                Title = "Tool / Set Enabled State",
                Description = "Enable or disable tools.",
                InputSchema = schemaDoc.RootElement.Clone()
            };
        }
    }
}

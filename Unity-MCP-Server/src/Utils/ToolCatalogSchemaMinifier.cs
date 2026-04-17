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
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;

namespace com.IvanMurzak.Unity.MCP.Server
{
    /// <summary>
    /// Minimizes the external MCP tool catalog without mutating the live in-process
    /// tool registry used by local Unity/editor consumers.
    /// </summary>
    public static class ToolCatalogSchemaMinifier
    {
        public static ListToolsResult Minimize(ListToolsResult result)
        {
            var tools = result.Tools ?? [];
            var minimizedTools = new List<Tool>(tools.Count);
            foreach (var tool in tools)
                minimizedTools.Add(Minimize(tool));

            var clone = CloneEnvelope(result);
            clone.Tools = minimizedTools;
            return clone;
        }

        public static Tool Minimize(Tool tool)
        {
#pragma warning disable MCPEXP001
            return new Tool
            {
                Name = tool.Name,
                Title = tool.Title,
                Description = tool.Description,
                InputSchema = Minimize(tool.InputSchema),
                OutputSchema = tool.OutputSchema,
                Annotations = tool.Annotations,
                Icons = tool.Icons == null ? null : [.. tool.Icons],
                Meta = tool.Meta?.DeepClone() as JsonObject,
                Execution = tool.Execution
            };
#pragma warning restore MCPEXP001
        }

        static JsonElement Minimize(JsonElement schema)
        {
            if (schema.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return schema;

            var node = JsonNode.Parse(schema.GetRawText());
            if (node == null)
                return schema;

            return JsonSerializer.SerializeToElement(Minimize(node, null));
        }

        static JsonNode? Minimize(JsonNode? node, string? parentPropertyName)
        {
            return node switch
            {
                null => null,
                JsonObject jsonObject => MinimizeObject(jsonObject, parentPropertyName),
                JsonArray jsonArray => MinimizeArray(jsonArray, parentPropertyName),
                _ => node.DeepClone()
            };
        }

        static JsonObject MinimizeObject(JsonObject jsonObject, string? parentPropertyName)
        {
            var clone = new JsonObject();
            var preserveNamedEntries = IsNamedSchemaContainer(parentPropertyName);
            foreach (var property in jsonObject)
            {
                if (!preserveNamedEntries && property.Key == "description")
                    continue;

                clone[property.Key] = Minimize(property.Value, property.Key);
            }

            return clone;
        }

        static JsonArray MinimizeArray(JsonArray jsonArray, string? parentPropertyName)
        {
            var clone = new JsonArray();
            foreach (var item in jsonArray)
                clone.Add(Minimize(item, parentPropertyName));

            return clone;
        }

        static bool IsNamedSchemaContainer(string? parentPropertyName)
        {
            return parentPropertyName is "properties" or "$defs" or "definitions" or "patternProperties" or "dependentSchemas";
        }

        static ListToolsResult CloneEnvelope(ListToolsResult result)
        {
            var clone = new ListToolsResult();
            foreach (var property in typeof(ListToolsResult).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanRead || !property.CanWrite || property.Name == nameof(ListToolsResult.Tools))
                    continue;

                var value = property.GetValue(result);
                property.SetValue(clone, ClonePropertyValue(value));
            }

            return clone;
        }

        static object? ClonePropertyValue(object? value)
        {
            return value switch
            {
                null => null,
                JsonNode jsonNode => jsonNode.DeepClone(),
                _ => value
            };
        }
    }
}

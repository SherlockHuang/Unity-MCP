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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Tool
    {
        public const string ToolGetDetailId = "tool-get-detail";
        internal const string ToolGetDetailInternalErrorMessage = "Tool detail is temporarily unavailable due to an internal error.";

        [Description("Structured tool lookup failure.")]
        public class ToolLookupFailureData
        {
            [JsonInclude, JsonPropertyName("code")]
            [Description("Machine-readable failure code.")]
            public string Code { get; set; } = string.Empty;

            [JsonInclude, JsonPropertyName("message")]
            [Description("Human-readable explanation for the lookup failure.")]
            public string Message { get; set; } = string.Empty;

            [JsonInclude, JsonPropertyName("matches")]
            [Description("Candidate tool names when the lookup is ambiguous.")]
            public string[]? Matches { get; set; }
        }

        [Description("Parsed tool input argument detail.")]
        public class ToolArgumentDetailData
        {
            [JsonInclude, JsonPropertyName("name")]
            [Description("Argument name.")]
            public string Name { get; set; } = string.Empty;

            [JsonInclude, JsonPropertyName("type")]
            [Description("Structural argument type.")]
            public string Type { get; set; } = "unknown";

            [JsonInclude, JsonPropertyName("required")]
            [Description("Whether the argument is required.")]
            public bool Required { get; set; }

            [JsonInclude, JsonPropertyName("description")]
            [Description("Argument description from the full schema, if available.")]
            public string? Description { get; set; }
        }

        [Description("Detailed metadata for a single MCP tool.")]
        public class ToolDetailResultData
        {
            [JsonInclude, JsonPropertyName("success")]
            [Description("True when the lookup resolved to exactly one tool.")]
            public bool Success { get; set; }

            [JsonInclude, JsonPropertyName("requestedName")]
            [Description("Original name provided by the caller.")]
            public string RequestedName { get; set; } = string.Empty;

            [JsonInclude, JsonPropertyName("resolvedName")]
            [Description("Resolved tool name when the lookup succeeds.")]
            public string? ResolvedName { get; set; }

            [JsonInclude, JsonPropertyName("failure")]
            [Description("Structured failure details when the lookup does not succeed.")]
            public ToolLookupFailureData? Failure { get; set; }

            [JsonInclude, JsonPropertyName("name")]
            [Description("Tool name.")]
            public string? Name { get; set; }

            [JsonInclude, JsonPropertyName("title")]
            [Description("Tool title.")]
            public string? Title { get; set; }

            [JsonInclude, JsonPropertyName("description")]
            [Description("Tool description.")]
            public string? Description { get; set; }

            [JsonInclude, JsonPropertyName("enabled")]
            [Description("Whether the tool is currently enabled.")]
            public bool? Enabled { get; set; }

            [JsonInclude, JsonPropertyName("tokenCount")]
            [Description("Cached token count reported by the MCP SDK.")]
            public int? TokenCount { get; set; }

            [JsonInclude, JsonPropertyName("readOnlyHint")]
            [Description("Read-only execution hint.")]
            public bool? ReadOnlyHint { get; set; }

            [JsonInclude, JsonPropertyName("idempotentHint")]
            [Description("Idempotent execution hint.")]
            public bool? IdempotentHint { get; set; }

            [JsonInclude, JsonPropertyName("destructiveHint")]
            [Description("Destructive execution hint.")]
            public bool? DestructiveHint { get; set; }

            [JsonInclude, JsonPropertyName("openWorldHint")]
            [Description("Open-world execution hint.")]
            public bool? OpenWorldHint { get; set; }

            [JsonInclude, JsonPropertyName("inputs")]
            [Description("Parsed input arguments from the full input schema.")]
            public ToolArgumentDetailData[]? Inputs { get; set; }

            [JsonInclude, JsonPropertyName("inputSchema")]
            [Description("Full input schema when explicitly requested.")]
            public JsonNode? InputSchema { get; set; }

            [JsonInclude, JsonPropertyName("outputSchema")]
            [Description("Full output schema when explicitly requested.")]
            public JsonNode? OutputSchema { get; set; }
        }

        [McpPluginTool
        (
            ToolGetDetailId,
            Title = "Tool / Get Detail",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("Get rich metadata for a single MCP tool, including parsed arguments and optional full schemas.")]
        public ToolDetailResultData GetDetail
        (
            [Description("Tool name to resolve. Exact match is preferred; case-insensitive lookup is supported.")]
            string? name = null,

            [Description("Include the full input and output schemas in the response. Default: false")]
            bool? includeSchemas = false,

            [Description("Include parsed input arguments derived from the full input schema. Default: true")]
            bool? includeParsedArguments = true
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var requestedName = name ?? string.Empty;

                try
                {
                    var toolManager = UnityMcpPluginEditor.Instance.Tools
                        ?? throw new InvalidOperationException(Error.ToolManagerNotAvailable());

                    var resolution = ResolveToolLookup(toolManager, requestedName);
                    if (resolution.Failure != null || string.IsNullOrEmpty(resolution.ResolvedName))
                        return CreateFailureResult(requestedName, resolution.Failure!);

                    var liveTool = toolManager.GetAllTools()
                        .FirstOrDefault(tool => string.Equals(tool.Name, resolution.ResolvedName, StringComparison.Ordinal));
                    if (liveTool == null)
                    {
                        return CreateFailureResult(
                            requestedName,
                            new ToolLookupFailureData
                            {
                                Code = "not-found",
                                Message = $"Tool '{resolution.ResolvedName}' is no longer registered."
                            });
                    }

                    var fullTool = liveTool;
                    var fullInputSchema = fullTool.InputSchema;

                    return new ToolDetailResultData
                    {
                        Success = true,
                        RequestedName = requestedName,
                        ResolvedName = fullTool.Name,
                        Name = fullTool.Name,
                        Title = fullTool.Title,
                        Description = fullTool.Description,
                        Enabled = toolManager.IsToolEnabled(liveTool.Name),
                        TokenCount = fullTool.TokenCount,
                        ReadOnlyHint = fullTool.ReadOnlyHint,
                        IdempotentHint = fullTool.IdempotentHint,
                        DestructiveHint = fullTool.DestructiveHint,
                        OpenWorldHint = fullTool.OpenWorldHint,
                        Inputs = includeParsedArguments != false
                            ? ParseToolArguments(fullInputSchema)
                            : null,
                        InputSchema = includeSchemas == true
                            ? fullInputSchema?.DeepClone()
                            : null,
                        OutputSchema = includeSchemas == true
                            ? fullTool.OutputSchema?.DeepClone()
                            : null
                    };
                }
                catch (Exception ex)
                {
                    return CreateInternalErrorResult(requestedName, ex);
                }
            });
        }

        static ToolDetailResultData CreateFailureResult(string requestedName, ToolLookupFailureData failure)
        {
            return new ToolDetailResultData
            {
                Success = false,
                RequestedName = requestedName,
                Failure = failure
            };
        }

        internal static ToolDetailResultData CreateInternalErrorResult(string requestedName, Exception ex)
        {
            UnityMcpPluginEditor.Instance.LogError(
                "{tool} failed for '{requestedName}': {exception}",
                typeof(Tool_Tool),
                ToolGetDetailId,
                requestedName,
                ex.ToString());

            return CreateFailureResult(
                requestedName,
                new ToolLookupFailureData
                {
                    Code = "internal-error",
                    Message = ToolGetDetailInternalErrorMessage
                });
        }

        static (string? ResolvedName, ToolLookupFailureData? Failure) ResolveToolLookup(
            IToolManager toolManager,
            string requestedName)
        {
            if (string.IsNullOrWhiteSpace(requestedName))
            {
                return (null, new ToolLookupFailureData
                {
                    Code = "empty-name",
                    Message = "Tool name is null, empty, or whitespace."
                });
            }

            var (exactLookup, caseInsensitiveLookup) = BuildToolLookup(toolManager);
            if (exactLookup.TryGetValue(requestedName, out var exactMatch))
                return (exactMatch, null);

            if (!caseInsensitiveLookup.TryGetValue(requestedName, out var matches) || matches.Count == 0)
            {
                return (null, new ToolLookupFailureData
                {
                    Code = "not-found",
                    Message = $"Tool '{requestedName}' was not found."
                });
            }

            if (matches.Count == 1)
                return (matches[0], null);

            var orderedMatches = matches
                .Distinct(StringComparer.Ordinal)
                .OrderBy(match => match, StringComparer.Ordinal)
                .ToArray();

            return (null, new ToolLookupFailureData
            {
                Code = "ambiguous",
                Message = $"Tool '{requestedName}' matched multiple tools.",
                Matches = orderedMatches
            });
        }

        static ToolArgumentDetailData[] ParseToolArguments(JsonNode? schema)
        {
            if (schema is not JsonObject schemaObject)
                return Array.Empty<ToolArgumentDetailData>();

            if (!schemaObject.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode) ||
                propertiesNode is not JsonObject propertiesObject)
            {
                return Array.Empty<ToolArgumentDetailData>();
            }

            var requiredNames = new HashSet<string>(StringComparer.Ordinal);
            if (schemaObject.TryGetPropertyValue(JsonSchema.Required, out var requiredNode) &&
                requiredNode is JsonArray requiredArray)
            {
                foreach (var entry in requiredArray)
                {
                    if (entry != null)
                        requiredNames.Add(entry.ToString());
                }
            }

            return propertiesObject
                .Select(property => new ToolArgumentDetailData
                {
                    Name = property.Key,
                    Type = DescribeSchemaType(schemaObject, property.Value),
                    Required = requiredNames.Contains(property.Key),
                    Description = ExtractDescription(property.Value)
                })
                .ToArray();
        }

        static string? ExtractDescription(JsonNode? schema)
        {
            return schema is JsonObject jsonObject &&
                   jsonObject.TryGetPropertyValue(JsonSchema.Description, out var descriptionNode) &&
                   descriptionNode != null
                ? descriptionNode.ToString()
                : null;
        }

        static string DescribeSchemaType(JsonObject rootSchema, JsonNode? schema)
        {
            return DescribeSchemaType(rootSchema, schema, new HashSet<string>(StringComparer.Ordinal));
        }

        static string DescribeSchemaType(JsonObject rootSchema, JsonNode? schema, HashSet<string> visitedRefs)
        {
            if (schema is not JsonObject jsonObject)
                return "unknown";

            if (jsonObject.TryGetPropertyValue(JsonSchema.Ref, out var refNode) &&
                refNode != null)
            {
                var refValue = refNode.ToString();
                if (visitedRefs.Add(refValue) && TryResolveLocalRef(rootSchema, refValue) is JsonObject resolvedObject)
                    return DescribeSchemaType(rootSchema, resolvedObject, visitedRefs);
            }

            if (jsonObject.TryGetPropertyValue(JsonSchema.Type, out var typeNode) &&
                typeNode != null)
            {
                if (typeNode is JsonValue typeValue)
                {
                    var rawType = typeValue.ToString();
                    if (string.Equals(rawType, JsonSchema.Array, StringComparison.Ordinal) &&
                        jsonObject.TryGetPropertyValue(JsonSchema.Items, out var itemsNode) &&
                        itemsNode != null)
                    {
                        return $"array<{DescribeSchemaType(rootSchema, itemsNode, visitedRefs)}>";
                    }

                    return rawType;
                }

                if (typeNode is JsonArray typeArray)
                {
                    var values = typeArray
                        .Where(item => item != null)
                        .Select(item => item!.ToString())
                        .ToArray();
                    if (values.Length > 0)
                        return string.Join(" | ", values);
                }
            }

            if (jsonObject.TryGetPropertyValue(JsonSchema.Items, out var nestedItems) &&
                nestedItems != null)
            {
                return $"array<{DescribeSchemaType(rootSchema, nestedItems, visitedRefs)}>";
            }

            if (jsonObject.TryGetPropertyValue(JsonSchema.Properties, out var nestedProperties) &&
                nestedProperties is JsonObject)
            {
                return JsonSchema.Object;
            }

            if (jsonObject.TryGetPropertyValue(JsonSchema.Enum, out var enumNode) &&
                enumNode is JsonArray)
            {
                return "enum";
            }

            return "unknown";
        }

        static JsonNode? TryResolveLocalRef(JsonObject rootSchema, string refValue)
        {
            if (!refValue.StartsWith("#/", StringComparison.Ordinal))
                return null;

            JsonNode? current = rootSchema;
            foreach (var rawSegment in refValue.Substring(2).Split('/'))
            {
                var segment = rawSegment
                    .Replace("~1", "/", StringComparison.Ordinal)
                    .Replace("~0", "~", StringComparison.Ordinal);

                current = current switch
                {
                    JsonObject jsonObject when jsonObject.TryGetPropertyValue(segment, out var child) => child,
                    JsonArray jsonArray when int.TryParse(segment, out var index) && index >= 0 && index < jsonArray.Count => jsonArray[index],
                    _ => null
                };

                if (current == null)
                    return null;
            }

            return current;
        }
    }
}

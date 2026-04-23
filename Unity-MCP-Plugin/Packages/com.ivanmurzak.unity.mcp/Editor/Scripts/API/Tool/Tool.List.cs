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
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Utils;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Tool
    {
        public const string ToolListId = "tool-list";

        static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

        [Description("Specifies what to include for tool input arguments.")]
        public enum InputRequest
        {
            [Description("Do not include input arguments.")]
            None = 0,

            [Description("Include input argument names only.")]
            Inputs = 1
        }

        [Description("MCP tool information.")]
        public class ToolInfoData
        {
            [JsonInclude, JsonPropertyName("name")]
            [Description("Tool name.")]
            public string Name { get; set; } = string.Empty;

            [JsonInclude, JsonPropertyName("inputs")]
            [Description("Tool input arguments.")]
            public ToolInputData[]? Inputs { get; set; }
        }

        [Description("MCP tool input argument.")]
        public class ToolInputData
        {
            [JsonInclude, JsonPropertyName("name")]
            [Description("Argument name.")]
            public string Name { get; set; } = string.Empty;
        }

        [McpPluginTool
        (
            ToolListId,
            Title = "Tool / List",
            ReadOnlyHint = true,
            IdempotentHint = true
        )]
        [Description("List available MCP tools for lightweight discovery. " +
            "Optionally filter by regex across tool names and argument names.")]
        public ToolInfoData[] List
        (
            [Description("Regex pattern to filter tools. " +
                "Matches against tool name and argument names.")]
            string? regexSearch = null,

            [Description("Include input argument names in the result. Default: None")]
            InputRequest? includeInputs = InputRequest.None
        )
        {
            return MainThread.Instance.Run(() =>
            {
                var toolManager = UnityMcpPluginEditor.Instance.Tools
                    ?? throw new InvalidOperationException(Error.ToolManagerNotAvailable());

                var allTools = toolManager.GetAllTools();
                if (allTools == null)
                    return Array.Empty<ToolInfoData>();

                Regex? regex = null;
                if (!string.IsNullOrWhiteSpace(regexSearch))
                {
                    try { regex = new Regex(regexSearch!, RegexOptions.IgnoreCase, RegexTimeout); }
                    catch (ArgumentException ex)
                    {
                        throw new ArgumentException($"Invalid regex pattern '{regexSearch}': {ex.Message}", nameof(regexSearch));
                    }
                }

                var inputsRequested = includeInputs ?? InputRequest.None;
                var needInputs = regex != null || inputsRequested != InputRequest.None;

                var results = new List<ToolInfoData>();

                foreach (var tool in allTools)
                {
                    List<ToolInputData>? inputs = null;

                    if (regex != null)
                    {
                        if (!MatchesTool(regex, tool, needInputs ? tool.InputSchema : null, out inputs))
                            continue;
                    }

                    var info = new ToolInfoData { Name = tool.Name };

                    if (inputsRequested != InputRequest.None)
                    {
                        inputs ??= ParseInputs(tool.InputSchema);
                        info.Inputs = BuildInputResults(inputs);
                    }

                    results.Add(info);
                }

                return results.ToArray();
            });
        }

        static bool MatchesTool(Regex regex, IRunTool tool, JsonNode? schema, out List<ToolInputData>? parsedInputs)
        {
            parsedInputs = null;

            if (regex.IsMatch(tool.Name ?? string.Empty))
                return true;

            if (schema == null)
                return false;

            parsedInputs = ParseInputs(schema);
            foreach (var input in parsedInputs)
            {
                if (regex.IsMatch(input.Name))
                    return true;
            }

            return false;
        }

        static ToolInputData[] BuildInputResults(List<ToolInputData> inputs)
        {
            var result = new ToolInputData[inputs.Count];
            for (int i = 0; i < inputs.Count; i++)
            {
                result[i] = new ToolInputData
                {
                    Name = inputs[i].Name
                };
            }
            return result;
        }

        static List<ToolInputData> ParseInputs(JsonNode? schema)
        {
            var result = new List<ToolInputData>();

            if (schema is not JsonObject schemaObject)
                return result;

            if (!schemaObject.TryGetPropertyValue(JsonSchema.Properties, out var propertiesNode))
                return result;

            if (propertiesNode is not JsonObject propertiesObject)
                return result;

            foreach (var (name, element) in propertiesObject)
            {
                result.Add(new ToolInputData
                {
                    Name = name
                });
            }

            return result;
        }
    }
}

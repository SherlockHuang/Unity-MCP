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
using System.Linq;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    internal static class TestResultResponseBuilder
    {
        public static TestRunResponse Build(
            IReadOnlyList<TestResultData> results,
            TestSummaryData summary,
            IReadOnlyList<TestLogEntry> logs,
            TestResultFormat resultFormat,
            bool includePassingTests,
            bool includeMessage,
            bool includeMessageStacktrace,
            bool includeLogs,
            bool includeLogsStacktrace,
            LogType minLogType)
        {
            var response = new TestRunResponse
            {
                Summary = summary,
                ResultFormat = resultFormat,
                Results = new List<TestResultData>()
            };

            if (resultFormat == TestResultFormat.Tree)
                response.ResultGroups = BuildGroups(results, includePassingTests, includeMessage, includeMessageStacktrace);
            else
                response.Results = BuildFlatResults(results, includePassingTests, includeMessage, includeMessageStacktrace);

            if (includeLogs && logs.Count > 0)
            {
                var minLogLevel = TestLogEntry.ToLogLevel(minLogType);
                response.Logs = logs
                    .Where(log => log.LogLevel >= minLogLevel)
                    .Select(log => includeLogsStacktrace
                        ? log
                        : new TestLogEntry(log.Type, log.Condition, null, log.Timestamp))
                    .ToList();
            }

            return response;
        }

        static List<TestResultData> BuildFlatResults(
            IReadOnlyList<TestResultData> results,
            bool includePassingTests,
            bool includeMessage,
            bool includeMessageStacktrace)
        {
            return results
                .Where(result => includePassingTests || result.Status != TestResultStatus.Passed)
                .Select(result => new TestResultData
                {
                    Name = result.Name,
                    Status = result.Status,
                    Duration = result.Duration,
                    Message = includeMessage ? result.Message : null,
                    StackTrace = includeMessageStacktrace ? result.StackTrace : null
                })
                .ToList();
        }

        static List<TestResultGroupData> BuildGroups(
            IReadOnlyList<TestResultData> results,
            bool includePassingTests,
            bool includeMessage,
            bool includeMessageStacktrace)
        {
            return results
                .Where(result => includePassingTests || result.Status != TestResultStatus.Passed)
                .Select(result => new
                {
                    Result = result,
                    Identity = TestIdentity.Parse(result.Name)
                })
                .GroupBy(item => new
                {
                    item.Identity.AssemblyName,
                    item.Identity.Namespace,
                    item.Identity.ClassName
                })
                .OrderBy(group => group.Key.AssemblyName, StringComparer.Ordinal)
                .ThenBy(group => group.Key.Namespace, StringComparer.Ordinal)
                .ThenBy(group => group.Key.ClassName, StringComparer.Ordinal)
                .Select(group =>
                {
                    var leaves = group
                        .OrderBy(item => item.Identity.MethodName, StringComparer.Ordinal)
                        .Select(item => new TestResultLeafData
                        {
                            MethodName = item.Identity.MethodName,
                            FullName = item.Result.Name,
                            Status = item.Result.Status,
                            Duration = item.Result.Duration,
                            Message = includeMessage ? item.Result.Message : null,
                            StackTrace = includeMessageStacktrace ? item.Result.StackTrace : null
                        })
                        .ToList();

                    return new TestResultGroupData
                    {
                        AssemblyName = group.Key.AssemblyName,
                        Namespace = group.Key.Namespace,
                        ClassName = group.Key.ClassName,
                        TotalTests = leaves.Count,
                        PassedTests = leaves.Count(leaf => leaf.Status == TestResultStatus.Passed),
                        FailedTests = leaves.Count(leaf => leaf.Status == TestResultStatus.Failed),
                        SkippedTests = leaves.Count(leaf => leaf.Status == TestResultStatus.Skipped),
                        Duration = TimeSpan.FromTicks(leaves.Sum(leaf => leaf.Duration.Ticks)),
                        Results = leaves
                    };
                })
                .ToList();
        }

        readonly struct TestIdentity
        {
            public readonly string AssemblyName;
            public readonly string Namespace;
            public readonly string ClassName;
            public readonly string MethodName;

            TestIdentity(string assemblyName, string namespaceName, string className, string methodName)
            {
                AssemblyName = assemblyName;
                Namespace = namespaceName;
                ClassName = className;
                MethodName = methodName;
            }

            public static TestIdentity Parse(string fullName)
            {
                if (string.IsNullOrWhiteSpace(fullName))
                    return new TestIdentity(string.Empty, string.Empty, string.Empty, string.Empty);

                var lastDot = fullName.LastIndexOf('.');
                if (lastDot < 0)
                    return new TestIdentity(string.Empty, string.Empty, string.Empty, fullName);

                var methodName = fullName[(lastDot + 1)..];
                var ownerName = fullName[..lastDot];
                var classDot = ownerName.LastIndexOf('.');

                if (classDot < 0)
                    return new TestIdentity(string.Empty, string.Empty, ownerName, methodName);

                return new TestIdentity(
                    assemblyName: string.Empty,
                    namespaceName: ownerName[..classDot],
                    className: ownerName[(classDot + 1)..],
                    methodName: methodName);
            }
        }
    }
}

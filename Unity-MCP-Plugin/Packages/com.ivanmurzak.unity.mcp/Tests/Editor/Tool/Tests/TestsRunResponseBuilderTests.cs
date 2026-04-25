/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak)              │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using com.IvanMurzak.Unity.MCP.Editor.API.TestRunner;
using NUnit.Framework;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Tests
{
    public class TestsRunResponseBuilderTests
    {
        [Test]
        public void Build_DefaultFlat_ReturnsLegacyResultsShape()
        {
            var response = Build(
                TestResultFormat.Flat,
                includePassingTests: false,
                includeMessage: true,
                includeStacktrace: false,
                includeLogs: false,
                includeLogsStacktrace: false);

            Assert.AreEqual(TestResultFormat.Flat, response.ResultFormat);
            Assert.IsNotNull(response.Results);
            Assert.AreEqual(2, response.Results.Count);
            Assert.IsNull(response.ResultGroups);
            Assert.AreEqual("WG.Game.DailyDungeon.Tests.DailyDungeonSettlementRoutingTests.Fails_WhenRouteMissing", response.Results[0].Name);
            Assert.AreEqual("expected route", response.Results[0].Message);
            Assert.IsNull(response.Results[0].StackTrace);
        }

        [Test]
        public void Build_Tree_GroupsByNamespaceAndClass()
        {
            var response = Build(
                TestResultFormat.Tree,
                includePassingTests: true,
                includeMessage: true,
                includeStacktrace: false,
                includeLogs: false,
                includeLogsStacktrace: false);

            Assert.AreEqual(TestResultFormat.Tree, response.ResultFormat);
            Assert.AreEqual(0, response.Results.Count);
            Assert.IsNotNull(response.ResultGroups);
            Assert.AreEqual(2, response.ResultGroups!.Count);

            var dailyDungeonGroup = response.ResultGroups[0];
            Assert.AreEqual(string.Empty, dailyDungeonGroup.AssemblyName);
            Assert.AreEqual("WG.Game.DailyDungeon.Tests", dailyDungeonGroup.Namespace);
            Assert.AreEqual("DailyDungeonSettlementRoutingTests", dailyDungeonGroup.ClassName);
            Assert.AreEqual(2, dailyDungeonGroup.TotalTests);
            Assert.AreEqual(1, dailyDungeonGroup.PassedTests);
            Assert.AreEqual(1, dailyDungeonGroup.FailedTests);
            Assert.AreEqual("Fails_WhenRouteMissing", dailyDungeonGroup.Results[0].MethodName);
            Assert.AreEqual("WG.Game.DailyDungeon.Tests.DailyDungeonSettlementRoutingTests.Fails_WhenRouteMissing", dailyDungeonGroup.Results[0].FullName);
            Assert.AreEqual("Passes_WhenRouteExists", dailyDungeonGroup.Results[1].MethodName);
        }

        [Test]
        public void Build_Tree_RespectsIncludeFlagsAndLogs()
        {
            var response = Build(
                TestResultFormat.Tree,
                includePassingTests: false,
                includeMessage: false,
                includeStacktrace: true,
                includeLogs: true,
                includeLogsStacktrace: false);

            Assert.IsNotNull(response.ResultGroups);
            Assert.AreEqual(2, response.ResultGroups!.Count);
            Assert.AreEqual(1, response.ResultGroups[0].Results.Count);
            var leaf = response.ResultGroups[0].Results[0];
            Assert.AreEqual(TestResultStatus.Failed, leaf.Status);
            Assert.IsNull(leaf.Message);
            Assert.AreEqual("route stack", leaf.StackTrace);

            Assert.IsNotNull(response.Logs);
            Assert.AreEqual(1, response.Logs!.Count);
            Assert.AreEqual(LogType.Warning, response.Logs[0].Type);
            Assert.IsNull(response.Logs[0].StackTrace);
        }

        [Test]
        public void ResultFormat_PlayerPrefsValue_RoundTripsForDeferredRunPersistence()
        {
            var original = TestResultCollector.GetPersistedResultFormat();
            try
            {
                TestResultCollector.PersistResultFormat(TestResultFormat.Tree);

                var parsed = TestResultCollector.GetPersistedResultFormat();

                Assert.AreEqual(TestResultFormat.Tree, parsed);
            }
            finally
            {
                TestResultCollector.PersistResultFormat(original);
            }
        }

        static TestRunResponse Build(
            TestResultFormat resultFormat,
            bool includePassingTests,
            bool includeMessage,
            bool includeStacktrace,
            bool includeLogs,
            bool includeLogsStacktrace)
            => TestResultResponseBuilder.Build(
                results: SampleResults(),
                summary: new TestSummaryData
                {
                    Status = TestRunStatus.Failed,
                    TotalTests = 3,
                    PassedTests = 1,
                    FailedTests = 1,
                    SkippedTests = 1,
                    Duration = TimeSpan.FromMilliseconds(18)
                },
                logs: SampleLogs(),
                resultFormat: resultFormat,
                includePassingTests: includePassingTests,
                includeMessage: includeMessage,
                includeMessageStacktrace: includeStacktrace,
                includeLogs: includeLogs,
                includeLogsStacktrace: includeLogsStacktrace,
                minLogType: LogType.Warning);

        static List<TestResultData> SampleResults()
            => new List<TestResultData>
            {
                new TestResultData
                {
                    Name = "WG.Game.DailyDungeon.Tests.DailyDungeonSettlementRoutingTests.Fails_WhenRouteMissing",
                    Status = TestResultStatus.Failed,
                    Duration = TimeSpan.FromMilliseconds(12),
                    Message = "expected route",
                    StackTrace = "route stack"
                },
                new TestResultData
                {
                    Name = "WG.Game.DailyDungeon.Tests.DailyDungeonSettlementRoutingTests.Passes_WhenRouteExists",
                    Status = TestResultStatus.Passed,
                    Duration = TimeSpan.FromMilliseconds(4)
                },
                new TestResultData
                {
                    Name = "WG.Game.Treasure.Tests.TreasurePresentationTests.Skips_WhenConfigMissing",
                    Status = TestResultStatus.Skipped,
                    Duration = TimeSpan.FromMilliseconds(2),
                    Message = "missing config"
                }
            };

        static List<TestLogEntry> SampleLogs()
            => new List<TestLogEntry>
            {
                new TestLogEntry(LogType.Log, "informational", "info stack"),
                new TestLogEntry(LogType.Warning, "warning", "warning stack")
            };
    }
}

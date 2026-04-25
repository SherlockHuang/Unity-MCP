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

namespace com.IvanMurzak.Unity.MCP.Editor.API.TestRunner
{
    public class TestResultGroupData
    {
        public string AssemblyName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public int SkippedTests { get; set; }
        public TimeSpan Duration { get; set; }
        public List<TestResultLeafData> Results { get; set; } = new List<TestResultLeafData>();
    }

    public class TestResultLeafData
    {
        public string MethodName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public TestResultStatus Status { get; set; } = TestResultStatus.Skipped;
        public TimeSpan Duration { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }
    }
}

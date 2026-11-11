using System;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Represents the metrics collected during the execution
    /// of a single experimental stage.
    /// </summary>
    public sealed class StageMetric
    {
        #region Experiment Information

        /// <summary>
        /// Experiment identifier.
        /// Example: E1, E2, E3...
        /// </summary>
        public string Experiment { get; init; } = string.Empty;

        /// <summary>
        /// Experiment scenario.
        /// Example:
        /// Original
        /// Compiled
        /// Backend
        /// Sync
        /// Async
        /// ColdStart
        /// WarmStart
        /// </summary>
        public string Scenario { get; init; } = string.Empty;

        /// <summary>
        /// Experiment iteration.
        /// </summary>
        public int Iteration { get; init; }

        #endregion

        #region Stage Information

        /// <summary>
        /// Stage name.
        /// Examples:
        /// Pipeline
        /// Import
        /// FeatureExtraction
        /// Diagnosis
        /// BPTrend
        /// HeartRate
        /// </summary>
        public string Stage { get; init; } = string.Empty;

        /// <summary>
        /// Parent stage.
        /// Null when this stage is the root.
        /// </summary>
        public string? ParentStage { get; init; }

        #endregion

        #region Timing

        /// <summary>
        /// Stage start timestamp (UTC).
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Stage end timestamp (UTC).
        /// </summary>
        public DateTime EndTime { get; init; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public double DurationMilliseconds { get; init; }

        #endregion

        #region Managed Memory (.NET)

        /// <summary>
        /// Managed memory before execution.
        /// </summary>
        public long ManagedMemoryBeforeBytes { get; init; }

        /// <summary>
        /// Managed memory after execution.
        /// </summary>
        public long ManagedMemoryAfterBytes { get; init; }

        /// <summary>
        /// Managed memory consumed during execution.
        /// </summary>
        public long ManagedMemoryDeltaBytes =>
            ManagedMemoryAfterBytes - ManagedMemoryBeforeBytes;

        /// <summary>
        /// Managed memory consumed (MB).
        /// </summary>
        public double ManagedMemoryDeltaMB =>
            ManagedMemoryDeltaBytes / 1024d / 1024d;

        #endregion

        #region Process Memory

        /// <summary>
        /// Working Set before execution.
        /// </summary>
        public long WorkingSetBeforeBytes { get; init; }

        /// <summary>
        /// Working Set after execution.
        /// </summary>
        public long WorkingSetAfterBytes { get; init; }

        /// <summary>
        /// Working Set delta.
        /// </summary>
        public long WorkingSetDeltaBytes =>
            WorkingSetAfterBytes - WorkingSetBeforeBytes;

        /// <summary>
        /// Working Set delta (MB).
        /// </summary>
        public double WorkingSetDeltaMB =>
            WorkingSetDeltaBytes / 1024d / 1024d;

        /// <summary>
        /// Private memory before execution.
        /// </summary>
        public long PrivateMemoryBeforeBytes { get; init; }

        /// <summary>
        /// Private memory after execution.
        /// </summary>
        public long PrivateMemoryAfterBytes { get; init; }

        /// <summary>
        /// Private memory delta.
        /// </summary>
        public long PrivateMemoryDeltaBytes =>
            PrivateMemoryAfterBytes - PrivateMemoryBeforeBytes;

        /// <summary>
        /// Private memory delta (MB).
        /// </summary>
        public double PrivateMemoryDeltaMB =>
            PrivateMemoryDeltaBytes / 1024d / 1024d;

        #endregion

        #region Thread Information

        /// <summary>
        /// Managed thread identifier.
        /// </summary>
        public int ManagedThreadId { get; init; }

        /// <summary>
        /// Indicates whether the execution
        /// occurred on a ThreadPool thread.
        /// </summary>
        public bool IsThreadPoolThread { get; init; }

        #endregion

        #region Execution Status

        /// <summary>
        /// Indicates whether the stage
        /// completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Exception message when the stage fails.
        /// Null if the execution completed successfully.
        /// </summary>
        public string? Exception { get; init; }

        #endregion
    }
}
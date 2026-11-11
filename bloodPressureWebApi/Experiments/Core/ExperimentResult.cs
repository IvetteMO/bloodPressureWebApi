using System;
using System.Collections.Generic;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Represents the result of a complete experiment execution.
    /// </summary>
    public sealed class ExperimentResult
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
        /// Iteration number.
        /// </summary>
        public int Iteration { get; init; }

        #endregion

        #region Execution Information

        /// <summary>
        /// Indicates whether the execution completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Exception generated during execution.
        /// Null if execution completed successfully.
        /// </summary>
        public string? Exception { get; init; }

        /// <summary>
        /// Execution start time (UTC).
        /// </summary>
        public DateTime StartTime { get; init; }

        /// <summary>
        /// Execution end time (UTC).
        /// </summary>
        public DateTime EndTime { get; init; }

        /// <summary>
        /// Total execution time in milliseconds.
        /// </summary>
        public double TotalExecutionMilliseconds { get; init; }

        #endregion

        #region Environment

        /// <summary>
        /// Indicates whether this execution corresponds
        /// to a cold start.
        /// </summary>
        public bool ColdStart { get; init; }

        /// <summary>
        /// Indicates whether the execution was asynchronous.
        /// </summary>
        public bool IsAsync { get; init; }

        /// <summary>
        /// Number of concurrent executions.
        /// </summary>
        public int Concurrency { get; init; }

        #endregion

        #region Stage Metrics

        /// <summary>
        /// Metrics collected for every measured stage.
        /// </summary>
        public IReadOnlyList<StageMetric> Metrics { get; init; } =
            Array.Empty<StageMetric>();

        #endregion
    }
}
namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Statistical summary of one metric collected
    /// for one execution stage.
    /// </summary>
    public sealed class StageStatistics
    {
        // =====================================================
        // IDENTIFICATION
        // =====================================================

        /// <summary>
        /// Stage name.
        /// Example:
        /// Import
        /// Pipeline
        /// FeatureExtraction
        /// Diagnosis
        /// </summary>
        public string Stage { get; init; } =
            string.Empty;

        public string Experiment { get; init; } = string.Empty;

        public string Scenario { get; init; } = string.Empty;

        public int IterationCount { get; init; }

        /// <summary>
        /// Metric analyzed.
        /// Example:
        /// DurationMilliseconds
        /// ManagedMemoryDeltaMB
        /// WorkingSetDeltaMB
        /// PrivateMemoryDeltaMB
        /// </summary>
        public string Metric { get; init; } =
            string.Empty;

        /// <summary>
        /// Unit of measurement.
        /// Example:
        /// ms
        /// MB
        /// bytes
        /// %
        /// </summary>
        public string Unit { get; init; } =
            string.Empty;

        // =====================================================
        // SAMPLE
        // =====================================================

        public int SampleCount { get; init; }

        // =====================================================
        // CENTRAL TENDENCY
        // =====================================================

        public double Mean { get; init; }

        public double Median { get; init; }

        // =====================================================
        // DISPERSION
        // =====================================================

        public double Minimum { get; init; }

        public double Maximum { get; init; }

        public double Range =>
            Maximum - Minimum;

        public double Variance { get; init; }

        public double StandardDeviation { get; init; }

        public double StandardError { get; init; }

        public double CoefficientOfVariation { get; init; }

        // =====================================================
        // PERCENTILES
        // =====================================================

        public double Percentile1 { get; init; }

        public double Percentile5 { get; init; }

        public double Percentile25 { get; init; }

        public double Percentile50 =>
            Median;

        public double Percentile75 { get; init; }

        public double Percentile95 { get; init; }

        public double Percentile99 { get; init; }

        // =====================================================
        // QUARTILES
        // =====================================================

        public double Q1 =>
            Percentile25;

        public double Q2 =>
            Median;

        public double Q3 =>
            Percentile75;

        public double InterquartileRange =>
            Q3 - Q1;

        // =====================================================
        // CONFIDENCE INTERVAL
        // =====================================================

        public double ConfidenceInterval95Lower { get; init; }

        public double ConfidenceInterval95Upper { get; init; }

        // =====================================================
        // EXECUTION
        // =====================================================

        /// <summary>
        /// Successful executions.
        /// </summary>
        public int SuccessCount { get; init; }

        /// <summary>
        /// Failed executions.
        /// </summary>
        public int FailureCount { get; init; }

        /// <summary>
        /// Success percentage.
        /// </summary>
        public double SuccessRate =>
            SampleCount == 0
                ? 0
                : SuccessCount * 100.0 / SampleCount;

        /// <summary>
        /// Failure percentage.
        /// </summary>
        public double FailureRate =>
            SampleCount == 0
                ? 0
                : FailureCount * 100.0 / SampleCount;
    }
}
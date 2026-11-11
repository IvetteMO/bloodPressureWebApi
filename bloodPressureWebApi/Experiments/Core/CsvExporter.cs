using System.Globalization;
using System.Text;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Exports experiment metrics and statistical summaries to CSV files.
    /// </summary>
    public static class CsvExporter
    {
        private static readonly CultureInfo Culture =
            CultureInfo.InvariantCulture;

        // =====================================================
        // RAW STAGE METRICS
        // =====================================================

        public static void ExportMetrics(
            IEnumerable<StageMetric> metrics,
            string filePath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            using StreamWriter writer =
                new(filePath, false, Encoding.UTF8);

            writer.WriteLine(
    "Experiment,Scenario,Iteration," +
    "Stage,ParentStage,Success," +
    "StartTime,EndTime,DurationMilliseconds," +
    "ManagedMemoryDeltaBytes,ManagedMemoryDeltaMB," +
    "WorkingSetDeltaBytes,WorkingSetDeltaMB," +
    "PrivateMemoryDeltaBytes,PrivateMemoryDeltaMB," +
    "ManagedThreadId,IsThreadPoolThread," +
    "Exception");

            foreach (StageMetric metric in metrics.OrderBy(x => x.Iteration)
.ThenBy(x => x.Stage))
            {
                writer.WriteLine(string.Join(",",
    Escape(metric.Experiment),
    Escape(metric.Scenario),
    metric.Iteration,

    Escape(metric.Stage),
    Escape(metric.ParentStage),

    metric.Success,

    metric.StartTime.ToString("O"),
    metric.EndTime.ToString("O"),

    metric.DurationMilliseconds.ToString(Culture),

    metric.ManagedMemoryDeltaBytes,
    metric.ManagedMemoryDeltaMB.ToString(Culture),

    metric.WorkingSetDeltaBytes,
    metric.WorkingSetDeltaMB.ToString(Culture),

    metric.PrivateMemoryDeltaBytes,
    metric.PrivateMemoryDeltaMB.ToString(Culture),

    metric.ManagedThreadId,
    metric.IsThreadPoolThread,

    Escape(metric.Exception)
                ));
            }
        }

        // =====================================================
        // STATISTICS
        // =====================================================

        public static void ExportStatistics(
            IEnumerable<StageStatistics> statistics,
            string filePath)
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(filePath)!);

            using StreamWriter writer =
                new(filePath, false, Encoding.UTF8);

            writer.WriteLine(
                "Stage," +
                "Metric," +
                "Unit," +
                "SampleCount," +
                "Mean," +
                "Median," +
                "Minimum," +
                "Maximum," +
                "Range," +
                "Variance," +
                "StandardDeviation," +
                "StandardError," +
                "CoefficientOfVariation," +
                "Percentile1," +
                "Percentile5," +
                "Percentile25," +
                "Percentile50," +
                "Percentile75," +
                "Percentile95," +
                "Percentile99," +
                "Q1," +
                "Q2," +
                "Q3," +
                "InterquartileRange," +
                "ConfidenceInterval95Lower," +
                "ConfidenceInterval95Upper," +
                "SuccessCount," +
                "FailureCount," +
                "SuccessRate," +
                "FailureRate");

            foreach (StageStatistics s in statistics)
            {
                writer.WriteLine(string.Join(",",
                    Escape(s.Stage),
                    Escape(s.Metric),
                    Escape(s.Unit),

                    s.SampleCount,

                    s.Mean.ToString(Culture),
                    s.Median.ToString(Culture),

                    s.Minimum.ToString(Culture),
                    s.Maximum.ToString(Culture),
                    s.Range.ToString(Culture),

                    s.Variance.ToString(Culture),
                    s.StandardDeviation.ToString(Culture),
                    s.StandardError.ToString(Culture),

                    s.CoefficientOfVariation.ToString(Culture),

                    s.Percentile1.ToString(Culture),
                    s.Percentile5.ToString(Culture),
                    s.Percentile25.ToString(Culture),
                    s.Percentile50.ToString(Culture),
                    s.Percentile75.ToString(Culture),
                    s.Percentile95.ToString(Culture),
                    s.Percentile99.ToString(Culture),

                    s.Q1.ToString(Culture),
                    s.Q2.ToString(Culture),
                    s.Q3.ToString(Culture),
                    s.InterquartileRange.ToString(Culture),

                    s.ConfidenceInterval95Lower.ToString(Culture),
                    s.ConfidenceInterval95Upper.ToString(Culture),

                    s.SuccessCount,
                    s.FailureCount,

                    s.SuccessRate.ToString(Culture),
                    s.FailureRate.ToString(Culture)
                ));
            }
        }

        // =====================================================
        // HELPERS
        // =====================================================

        private static string Escape(
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Replace("\"", "\"\"");

            return $"\"{value}\"";
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace bloodPressureWebApi.Experiments.Core
{
    /// <summary>
    /// Calculates descriptive statistics for any numerical
    /// property contained in StageMetric.
    /// </summary>
    public static class StatisticsService
    {
        // =====================================================
        // PUBLIC
        // =====================================================

        public static StageStatistics Calculate(
            IEnumerable<StageMetric> metrics,
            Func<StageMetric, double> selector,
            string stage,
            string metric,
            string unit)
        {
            var metricList =
                metrics.ToList();

            var values =
                metricList
                    .Select(selector)
                    .OrderBy(x => x)
                    .ToArray();

            if (values.Length == 0)
            {
                throw new InvalidOperationException(
                    "No data available.");
            }

            double mean =
                values.Average();

            double variance =
                values.Sum(
                    x => Math.Pow(
                        x - mean,
                        2))
                / values.Length;

            double standardDeviation =
                Math.Sqrt(
                    variance);

            double standardError =
                standardDeviation
                /
                Math.Sqrt(
                    values.Length);

            double margin95 =
                1.96
                *
                standardError;

            StageMetric first =
    metricList.First();

            return new StageStatistics
            {
                // =============================================
                // IDENTIFICATION
                // =============================================

                Experiment = first.Experiment,

                Scenario = first.Scenario,

                IterationCount =
        metricList
            .Select(x => x.Iteration)
            .Distinct()
            .Count(),

                Stage = stage,

                Metric = metric,

                Unit = unit,

                // =============================================
                // SAMPLE
                // =============================================

                SampleCount =
                    values.Length,

                // =============================================
                // CENTRAL TENDENCY
                // =============================================

                Mean =
                    mean,

                Median =
                    Percentile(values, 50),

                // =============================================
                // DISPERSION
                // =============================================

                Minimum =
                    values.First(),

                Maximum =
                    values.Last(),

                Variance =
                    variance,

                StandardDeviation =
                    standardDeviation,

                StandardError =
                    standardError,

                CoefficientOfVariation =
                    mean == 0
                        ? 0
                        : standardDeviation
                          / mean
                          * 100,

                // =============================================
                // PERCENTILES
                // =============================================

                Percentile1 =
                    Percentile(values, 1),

                Percentile5 =
                    Percentile(values, 5),

                Percentile25 =
                    Percentile(values, 25),

                Percentile75 =
                    Percentile(values, 75),

                Percentile95 =
                    Percentile(values, 95),

                Percentile99 =
                    Percentile(values, 99),

                // =============================================
                // CONFIDENCE INTERVAL
                // =============================================

                ConfidenceInterval95Lower =
                    mean - margin95,

                ConfidenceInterval95Upper =
                    mean + margin95,

                // =============================================
                // EXECUTION
                // =============================================

                SuccessCount =
                    metricList.Count(
                        x => x.Success),

                FailureCount =
                    metricList.Count(
                        x => !x.Success)
            };
        }

        // =====================================================
        // PRIVATE
        // =====================================================

        private static double Percentile(
            IReadOnlyList<double> sortedValues,
            double percentile)
        {
            if (sortedValues.Count == 1)
            {
                return sortedValues[0];
            }

            double position =
                (sortedValues.Count - 1)
                *
                percentile
                /
                100.0;

            int lower =
                (int)Math.Floor(position);

            int upper =
                (int)Math.Ceiling(position);

            if (lower == upper)
            {
                return sortedValues[lower];
            }

            double weight =
                position - lower;

            return
                sortedValues[lower]
                +
                (
                    sortedValues[upper]
                    -
                    sortedValues[lower]
                )
                *
                weight;
        }

        // =====================================================
        // PREDEFINED METRICS
        // =====================================================

        public static IEnumerable<StageStatistics>
            CalculateAll(
                IEnumerable<StageMetric> metrics)
        {
            foreach (var stageGroup in metrics.GroupBy(x => x.Stage))
            {
                yield return Calculate(
                    stageGroup,
                    x => x.DurationMilliseconds,
                    stageGroup.Key,
                    "DurationMilliseconds",
                    "ms");

                yield return Calculate(
                    stageGroup,
                    x => x.ManagedMemoryDeltaMB,
                    stageGroup.Key,
                    "ManagedMemoryDeltaMB",
                    "MB");

                yield return Calculate(
                    stageGroup,
                    x => x.WorkingSetDeltaMB,
                    stageGroup.Key,
                    "WorkingSetDeltaMB",
                    "MB");

                yield return Calculate(
                    stageGroup,
                    x => x.PrivateMemoryDeltaMB,
                    stageGroup.Key,
                    "PrivateMemoryDeltaMB",
                    "MB");

                yield return Calculate(
                    stageGroup,
                    x => x.ManagedMemoryDeltaBytes,
                    stageGroup.Key,
                    "ManagedMemoryDeltaBytes",
                    "bytes");

                yield return Calculate(
                    stageGroup,
                    x => x.WorkingSetDeltaBytes,
                    stageGroup.Key,
                    "WorkingSetDeltaBytes",
                    "bytes");

                yield return Calculate(
                    stageGroup,
                    x => x.PrivateMemoryDeltaBytes,
                    stageGroup.Key,
                    "PrivateMemoryDeltaBytes",
                    "bytes");
            }
        }
    }
}
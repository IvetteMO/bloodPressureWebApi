
using bloodPressureWebApi.DTOs.E1;
using bloodPressureWebApi.Models;

namespace bloodPressureWebApi.Services
{
    public class E1BAnalysisService
    {
        public E1BStatisticsDto Analyze(
            Guid batchId,
            List<E1BValidationResult> results)
        {
            if (results == null || results.Count == 0)
                throw new InvalidOperationException(
                    "No E1-B results are available for statistical analysis.");

            var statistics = new E1BStatisticsDto
            {
                BatchId = batchId,
                ExperimentCode = "E1-B",
                ExperimentVersion = "E1-B_v1.0",
                Studies = results.Count
            };

            AddMetric(statistics, results, "SysANN", "mmHg",
                x => x.MatlabSysANN, x => x.BackendSysANN);

            AddMetric(statistics, results, "DiaANN", "mmHg",
                x => x.MatlabDiaANN, x => x.BackendDiaANN);

            AddMetric(statistics, results, "PulseANN", "bpm",
                x => x.MatlabPulseANN, x => x.BackendPulseANN);

            AddMetric(statistics, results, "BPLevel", "fuzzy output",
                x => x.MatlabBPLevel, x => x.BackendBPLevel);

            AddMetric(statistics, results, "HeartRate", "fuzzy output",
                x => x.MatlabHeartRate, x => x.BackendHeartRate);

            AddMetric(statistics, results, "DaySystolicMean", "mmHg",
                x => x.MatlabDaySys, x => x.BackendDaySys);

            AddMetric(statistics, results, "DayDiastolicMean", "mmHg",
                x => x.MatlabDayDia, x => x.BackendDayDia);

            AddMetric(statistics, results, "NightSystolicMean", "mmHg",
                x => x.MatlabNightSys, x => x.BackendNightSys);

            AddMetric(statistics, results, "NightDiastolicMean", "mmHg",
                x => x.MatlabNightDia, x => x.BackendNightDia);

            AddMetric(statistics, results, "Csis", "ratio",
                x => x.MatlabCsis, x => x.BackendCsis);

            AddMetric(statistics, results, "Cdia", "ratio",
                x => x.MatlabCdia, x => x.BackendCdia);

            AddMetric(statistics, results, "NocturnalOutput", "fuzzy output",
                x => x.MatlabNocturnalOutput,
                x => x.BackendNocturnalOutput);

            return statistics;
        }

        private static void AddMetric(
            E1BStatisticsDto statistics,
            List<E1BValidationResult> results,
            string metricName,
            string unit,
            Func<E1BValidationResult, double> matlabSelector,
            Func<E1BValidationResult, double> backendSelector)
        {
            var pairs = results
                .Select(x => (
                    Matlab: matlabSelector(x),
                    Backend: backendSelector(x)))
                .Where(x =>
                    double.IsFinite(x.Matlab) &&
                    double.IsFinite(x.Backend))
                .ToList();

            if (pairs.Count == 0)
                return;

            var differences = pairs
                .Select(x => x.Backend - x.Matlab)
                .ToArray();

            var absoluteErrors = differences
                .Select(Math.Abs)
                .OrderBy(x => x)
                .ToArray();

            int n = pairs.Count;

            double mae = absoluteErrors.Average();

            double rmse = Math.Sqrt(
                differences.Average(x => x * x));

            double bias = differences.Average();

            double sd = n > 1
                ? Math.Sqrt(
                    differences.Sum(x => (x - bias) * (x - bias))
                    / (n - 1))
                : 0.0;

            double? mape = null;

            var percentageErrors = pairs
                .Where(x => Math.Abs(x.Matlab) > 1e-12)
                .Select(x =>
                    Math.Abs(x.Backend - x.Matlab)
                    / Math.Abs(x.Matlab) * 100.0)
                .ToList();

            if (percentageErrors.Count > 0)
                mape = percentageErrors.Average();

            double meanMatlab = pairs.Average(x => x.Matlab);
            double meanBackend = pairs.Average(x => x.Backend);

            double covariance = pairs.Sum(x =>
                (x.Matlab - meanMatlab)
                * (x.Backend - meanBackend));

            double sumSquaresMatlab = pairs.Sum(x =>
                Math.Pow(x.Matlab - meanMatlab, 2));

            double sumSquaresBackend = pairs.Sum(x =>
                Math.Pow(x.Backend - meanBackend, 2));

            double pearsonR =
                sumSquaresMatlab > 0 && sumSquaresBackend > 0
                ? covariance /
                    Math.Sqrt(sumSquaresMatlab * sumSquaresBackend)
                : double.NaN;

            double medianAbsoluteError =
                n % 2 == 1
                ? absoluteErrors[n / 2]
                : (absoluteErrors[n / 2 - 1]
                   + absoluteErrors[n / 2]) / 2.0;

            statistics.Metrics.Add(new E1BMetricDto
            {
                Metric = metricName,
                Unit = unit,
                N = n,
                Mae = mae,
                Rmse = rmse,
                MeanBias = bias,
                Mape = mape,
                StandardDeviation = sd,
                PearsonR = pearsonR,
                MedianAbsoluteError = medianAbsoluteError,
                LowerAgreementLimit = bias - 1.96 * sd,
                UpperAgreementLimit = bias + 1.96 * sd,
                MaxAbsoluteError = absoluteErrors.Max()
            });
        }
    }
}

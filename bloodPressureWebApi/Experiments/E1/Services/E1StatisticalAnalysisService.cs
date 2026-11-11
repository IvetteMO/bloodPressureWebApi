using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    public class E1StatisticalAnalysisService
    {
        private readonly
            ApplicationDbContext _context;


        public E1StatisticalAnalysisService(
            ApplicationDbContext context)
        {
            _context =
                context;
        }


        // =====================================================
        // COMPLETE STATISTICAL ANALYSIS
        // =====================================================

        public async Task<E1StatisticalAnalysisDto>
            AnalyzeBatch(
                Guid batchId)
        {
            var results =
                await _context
                    .MAPAValidationResults
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.BatchId == batchId)
                    .OrderBy(
                        x =>
                            x.StudyId)
                    .ToListAsync();


            if (
                results.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No E1 results found for BatchId={batchId}.");
            }


            var analysis =
                new E1StatisticalAnalysisDto
                {
                    ExperimentCode =
                        "E1-A",

                    ExperimentVersion =
                        "E1-A_v1.0",

                    BatchId =
                        batchId,

                    Studies =
                        results.Count
                };


            // =================================================
            // COMPUTATIONAL VARIABLES
            // =================================================

            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Day Systolic Mean",

                    "mmHg",

                    results,

                    x =>
                        x.OriginalDaySystolicMean,

                    x =>
                        x.BackendDaySystolicMean));


            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Day Diastolic Mean",

                    "mmHg",

                    results,

                    x =>
                        x.OriginalDayDiastolicMean,

                    x =>
                        x.BackendDayDiastolicMean));


            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Night Systolic Mean",

                    "mmHg",

                    results,

                    x =>
                        x.OriginalNightSystolicMean,

                    x =>
                        x.BackendNightSystolicMean));


            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Night Diastolic Mean",

                    "mmHg",

                    results,

                    x =>
                        x.OriginalNightDiastolicMean,

                    x =>
                        x.BackendNightDiastolicMean));


            analysis.Metrics.Add(

                AnalyzeMetric(

                    "24h Mean Pulse",

                    "bpm",

                    results,

                    x =>
                        x.OriginalMeanPulse,

                    x =>
                        x.BackendMeanPulse));


            // =================================================
            // DATA FIDELITY VARIABLES
            // =================================================

            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Day Pressure Load",

                    "percentage points",

                    results,

                    x =>
                        x.OriginalDayPressureLoad,

                    x =>
                        x.BackendDayPressureLoad));


            analysis.Metrics.Add(

                AnalyzeMetric(

                    "Night Pressure Load",

                    "percentage points",

                    results,

                    x =>
                        x.OriginalNightPressureLoad,

                    x =>
                        x.BackendNightPressureLoad));


            analysis.Complete =
                analysis.Metrics.All(
                    x =>
                        x.N == results.Count);


            return analysis;
        }

        public async Task ExportComparisonDatasetAsync(
    Guid batchId,
    string outputCsv)
        {
            var results = await _context
                .MAPAValidationResults
                .AsNoTracking()
                .Where(x => x.BatchId == batchId)
                .OrderBy(x => x.StudyId)
                .ToListAsync();

            if (!results.Any())
                throw new InvalidOperationException(
                    $"No comparison data found for BatchId {batchId}");

            using StreamWriter writer = new(outputCsv);

            writer.WriteLine(
                "StudyId," +
                "FileName," +

                "MatlabDaySys,BackendDaySys,ErrorDaySys," +

                "MatlabDayDia,BackendDayDia,ErrorDayDia," +

                "MatlabNightSys,BackendNightSys,ErrorNightSys," +

                "MatlabNightDia,BackendNightDia,ErrorNightDia," +

                "MatlabMeanPulse,BackendMeanPulse,ErrorMeanPulse," +

                "MatlabDayLoad,BackendDayLoad,ErrorDayLoad," +

                "MatlabNightLoad,BackendNightLoad,ErrorNightLoad");

            foreach (var r in results)
            {
                writer.WriteLine(
                    $"{r.StudyId}," +
                    $"{r.SourceFileName}," +

                    $"{r.OriginalDaySystolicMean}," +
                    $"{r.BackendDaySystolicMean}," +
                    $"{r.DaySystolicAbsoluteError}," +

                    $"{r.OriginalDayDiastolicMean}," +
                    $"{r.BackendDayDiastolicMean}," +
                    $"{r.DayDiastolicAbsoluteError}," +

                    $"{r.OriginalNightSystolicMean}," +
                    $"{r.BackendNightSystolicMean}," +
                    $"{r.NightSystolicAbsoluteError}," +

                    $"{r.OriginalNightDiastolicMean}," +
                    $"{r.BackendNightDiastolicMean}," +
                    $"{r.NightDiastolicAbsoluteError}," +

                    $"{r.OriginalMeanPulse}," +
                    $"{r.BackendMeanPulse}," +
                    $"{r.MeanPulseAbsoluteError}," +

                    $"{r.OriginalDayPressureLoad}," +
                    $"{r.BackendDayPressureLoad}," +
                    $"{r.DayPressureLoadAbsoluteError}," +

                    $"{r.OriginalNightPressureLoad}," +
                    $"{r.BackendNightPressureLoad}," +
                    $"{r.NightPressureLoadAbsoluteError}");
            }
        }

        // =====================================================
        // ANALYZE ONE METRIC
        // =====================================================

        private static E1StatisticalMetricDto AnalyzeMetric(
                string metric,
                string unit,
                List<MAPAValidationResult> results,
                Func<MAPAValidationResult, double?> referenceSelector,
                Func<MAPAValidationResult, double?> backendSelector)
        {
            var pairs =
                results

                    .Select(
                        x =>
                            new
                            {
                                StudyId =
                                    x.StudyId,

                                Patient =
                                    x.ExternalPatientCode,

                                File =
                                    x.SourceFileName,

                                Reference =
                                    referenceSelector(x),

                                Backend =
                                    backendSelector(x)
                            })

                    .Where(
                        x =>
                            x.Reference.HasValue
                            &&
                            x.Backend.HasValue)

                    .Select(
                        x =>
                            new E1Pair
                            {
                                StudyId =
                                    x.StudyId,

                                ExternalPatientCode =
                                    x.Patient,

                                SourceFileName =
                                    x.File,

                                Reference =
                                    x.Reference!.Value,

                                Backend =
                                    x.Backend!.Value
                            })

                    .ToList();


            if (
                pairs.Count == 0)
            {
                return new E1StatisticalMetricDto
                {
                    Metric =
                        metric,

                    Unit =
                        unit,

                    N =
                        0
                };
            }


            List<double> referenceValues =
                pairs
                    .Select(
                        x =>
                            x.Reference)
                    .ToList();


            List<double> backendValues =
                pairs
                    .Select(
                        x =>
                            x.Backend)
                    .ToList();


            // Backend - Reference

            List<double> differences =
                pairs
                    .Select(
                        x =>
                            x.Backend
                            -
                            x.Reference)
                    .ToList();


            List<double> absoluteErrors =
                differences
                    .Select(
                        Math.Abs)
                    .ToList();


            double referenceMean =
                referenceValues.Average();


            double backendMean =
                backendValues.Average();


            double referenceSd =
                SampleStandardDeviation(
                    referenceValues);


            double backendSd =
                SampleStandardDeviation(
                    backendValues);


            double mae =
                absoluteErrors.Average();


            double rmse =
                Math.Sqrt(

                    differences

                        .Select(
                            x =>
                                x * x)

                        .Average());

            double mape =
    pairs
        .Where(x => Math.Abs(x.Reference) > double.Epsilon)
        .Average(x =>
            Math.Abs(
                (x.Backend - x.Reference)
                / x.Reference) * 100.0);


            double bias =
                differences.Average();


            double differenceSd =
                SampleStandardDeviation(
                    differences);




            // =================================================
            // BLAND-ALTMAN
            // =================================================

            double lowerLoA =
                bias
                -
                1.96
                *
                differenceSd;


            double upperLoA =
                bias
                +
                1.96
                *
                differenceSd;


            // =================================================
            // PEARSON
            // =================================================

            double pearson =
                Pearson(
                    referenceValues,
                    backendValues);


            double rSquared =
                pearson
                *
                pearson;


            // =================================================
            // 95% CI FOR BIAS
            //
            // Normal approximation:
            //
            // bias ± 1.96 * SE
            // =================================================

            double standardErrorBias =
                differenceSd
                /
                Math.Sqrt(
                    pairs.Count);


            double biasCiLower =
                bias
                -
                1.96
                *
                standardErrorBias;


            double biasCiUpper =
                bias
                +
                1.96
                *
                standardErrorBias;


            // =================================================
            // BOOTSTRAP 95% CI FOR MAE
            //
            // Deterministic seed for reproducibility.
            // =================================================

            var maeBootstrap =
                BootstrapMae(

                    absoluteErrors,

                    iterations:
                        10000,

                    seed:
                        20260722);


            // =================================================
            // OUTLIER / MAX ERROR
            // =================================================

            E1Pair maximumErrorPair =
                pairs

                    .OrderByDescending(
                        x =>
                            Math.Abs(
                                x.Backend
                                -
                                x.Reference))

                    .First();


            double maxAbsoluteError =
                Math.Abs(
                    maximumErrorPair.Backend
                    -
                    maximumErrorPair.Reference);

            double medianAE =  Median(absoluteErrors);

            double minAbsoluteError =
                absoluteErrors.Min();

            const double tolerance = 0.001;

            double agreement =
                pairs.Count(x =>
                    Math.Abs(
                        x.Backend -
                        x.Reference)
                    <= tolerance)

                * 100.0

                / pairs.Count;


            return new E1StatisticalMetricDto
            {
                Metric =
                    metric,

                Unit =
                    unit,

                N =
                    pairs.Count,


                ReferenceMean =
                    referenceMean,

                ReferenceStandardDeviation =
                    referenceSd,


                BackendMean =
                    backendMean,

                BackendStandardDeviation =
                    backendSd,


                Mae =
                    mae,

                Mape = mape,

                MedianAbsoluteError = medianAE,

                MinAbsoluteError = minAbsoluteError,

                AgreementPercentage = agreement,

                MaeCi95Lower =
                    maeBootstrap.Lower,

                MaeCi95Upper =
                    maeBootstrap.Upper,


                Rmse =
                    rmse,


                MeanBias =
                    bias,

                BiasCi95Lower =
                    biasCiLower,

                BiasCi95Upper =
                    biasCiUpper,


                DifferenceStandardDeviation =
                    differenceSd,


                LowerAgreementLimit =
                    lowerLoA,

                UpperAgreementLimit =
                    upperLoA,


                PearsonR =
                    pearson,

                RSquared =
                    rSquared,


                MaxAbsoluteError =
                    maxAbsoluteError,

                MaxErrorStudyId =
                    maximumErrorPair.StudyId,

                MaxErrorPatientCode =
                    maximumErrorPair
                        .ExternalPatientCode,

                MaxErrorSourceFile =
                    maximumErrorPair
                        .SourceFileName,




            };
        }


        // =====================================================
        // SAMPLE STANDARD DEVIATION
        // =====================================================

        private static double
            SampleStandardDeviation(
                IReadOnlyCollection<double> values)
        {
            if (
                values.Count <= 1)
            {
                return 0;
            }


            double mean =
                values.Average();


            double sum =
                values.Sum(

                    x =>
                        Math.Pow(
                            x - mean,
                            2));


            return Math.Sqrt(

                sum
                /
                (
                    values.Count
                    -
                    1
                ));
        }


        // =====================================================
        // PEARSON CORRELATION
        // =====================================================

        private static double
            Pearson(
                IReadOnlyList<double> x,
                IReadOnlyList<double> y)
        {
            if (
                x.Count != y.Count
                ||
                x.Count < 2)
            {
                return 0;
            }


            double meanX =
                x.Average();


            double meanY =
                y.Average();


            double numerator =
                0;


            double denominatorX =
                0;


            double denominatorY =
                0;


            for (
                int i = 0;
                i < x.Count;
                i++)
            {
                double dx =
                    x[i]
                    -
                    meanX;


                double dy =
                    y[i]
                    -
                    meanY;


                numerator +=
                    dx
                    *
                    dy;


                denominatorX +=
                    dx
                    *
                    dx;


                denominatorY +=
                    dy
                    *
                    dy;
            }


            double denominator =
                Math.Sqrt(

                    denominatorX
                    *
                    denominatorY);


            if (
                denominator
                <=
                double.Epsilon)
            {
                return 0;
            }


            return numerator
                /
                denominator;
        }


        
        // =====================================================
        // BOOTSTRAP MAE
        // =====================================================



        private static BootstrapCi
            BootstrapMae(
                IReadOnlyList<double> absoluteErrors,
                int iterations,
                int seed)
        {
            if (
                absoluteErrors.Count == 0)
            {
                return new BootstrapCi();
            }


            Random random =
                new(
                    seed);


            double[] bootstrapMeans =
                new double[
                    iterations];


            for (
                int iteration = 0;
                iteration < iterations;
                iteration++)
            {
                double sum =
                    0;


                for (
                    int sample = 0;
                    sample < absoluteErrors.Count;
                    sample++)
                {
                    int index =
                        random.Next(
                            absoluteErrors.Count);


                    sum +=
                        absoluteErrors[
                            index];
                }


                bootstrapMeans[
                    iteration]
                    =
                    sum
                    /
                    absoluteErrors.Count;
            }


            Array.Sort(
                bootstrapMeans);


            int lowerIndex =
                (int)Math.Floor(

                    0.025
                    *
                    (
                        iterations
                        -
                        1
                    ));


            int upperIndex =
                (int)Math.Ceiling(

                    0.975
                    *
                    (
                        iterations
                        -
                        1
                    ));


            return new BootstrapCi
            {
                Lower =
                    bootstrapMeans[
                        lowerIndex],

                Upper =
                    bootstrapMeans[
                        upperIndex]
            };
        }

        // =====================================================
        // MEDIAN
        // =====================================================

        private static double Median(
            IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return 0;
            }

            List<double> ordered =
                values
                    .OrderBy(x => x)
                    .ToList();

            int middle =
                ordered.Count / 2;

            if (ordered.Count % 2 == 0)
            {
                return
                    (
                        ordered[middle - 1]
                        +
                        ordered[middle]
                    ) / 2.0;
            }

            return ordered[middle];
        }


        // =====================================================
        // INTERNAL PAIR
        // =====================================================

        private class E1Pair
        {
            public int StudyId
            {
                get;
                set;
            }


            public string?
                ExternalPatientCode
            {
                get;
                set;
            }


            public string?
                SourceFileName
            {
                get;
                set;
            }


            public double Reference
            {
                get;
                set;
            }


            public double Backend
            {
                get;
                set;
            }
        }


        private class BootstrapCi
        {
            public double Lower
            {
                get;
                set;
            }


            public double Upper
            {
                get;
                set;
            }
        }
    }


    // =========================================================
    // COMPLETE ANALYSIS DTO
    // =========================================================

    public class E1StatisticalAnalysisDto
    {
        public string ExperimentCode
        {
            get;
            set;
        }
        =
        string.Empty;


        public string ExperimentVersion
        {
            get;
            set;
        }
        =
        string.Empty;


        public Guid BatchId
        {
            get;
            set;
        }


        public int Studies
        {
            get;
            set;
        }


        public bool Complete
        {
            get;
            set;
        }


        public List<E1StatisticalMetricDto>
            Metrics
        {
            get;
            set;
        }
        =
        new();
    }


    // =========================================================
    // METRIC DTO
    // =========================================================

    public class E1StatisticalMetricDto
    {
        public string Metric
        {
            get;
            set;
        }
        =
        string.Empty;


        public string Unit
        {
            get;
            set;
        }
        =
        string.Empty;


        public int N
        {
            get;
            set;
        }


        public double?
            ReferenceMean
        {
            get;
            set;
        }


        public double?
            ReferenceStandardDeviation
        {
            get;
            set;
        }


        public double?
            BackendMean
        {
            get;
            set;
        }


        public double?
            BackendStandardDeviation
        {
            get;
            set;
        }


        public double?
            Mae
        {
            get;
            set;
        }


        public double?
            MaeCi95Lower
        {
            get;
            set;
        }


        public double?
            MaeCi95Upper
        {
            get;
            set;
        }


        public double?
            Rmse
        {
            get;
            set;
        }


        public double?
            MeanBias
        {
            get;
            set;
        }


        public double?
            BiasCi95Lower
        {
            get;
            set;
        }


        public double?
            BiasCi95Upper
        {
            get;
            set;
        }


        public double?
            DifferenceStandardDeviation
        {
            get;
            set;
        }


        public double?
            LowerAgreementLimit
        {
            get;
            set;
        }


        public double?
            UpperAgreementLimit
        {
            get;
            set;
        }


        public double?
            PearsonR
        {
            get;
            set;
        }


        public double?
            RSquared
        {
            get;
            set;
        }


        public double?
            MaxAbsoluteError
        {
            get;
            set;
        }


        public int?
            MaxErrorStudyId
        {
            get;
            set;
        }


        public string?
            MaxErrorPatientCode
        {
            get;
            set;
        }


        public string?
            MaxErrorSourceFile
        {
            get;
            set;
        }

        public double? Mape { get; set; }

        public double? MedianAbsoluteError { get; set; }

        public double? MinAbsoluteError { get; set; }

        public double? AgreementPercentage { get; set; }
    }
}
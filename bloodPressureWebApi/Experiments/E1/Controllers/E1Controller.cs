using System.Globalization;
using System.Text;

using bloodPressureWebApi.Data;
using bloodPressureWebApi.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route(
        "api/experiments/e1")]

    public class E1Controller
        : ControllerBase
    {
        // =====================================================
        // DEPENDENCIES
        // =====================================================

        private readonly
            ApplicationDbContext
            _context;


        private readonly
    E1StatisticalAnalysisService
    _statisticalAnalysisService;


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public E1Controller(
    ApplicationDbContext context,


    E1StatisticalAnalysisService
        statisticalAnalysisService)
        {
            _context =
                context;


            _statisticalAnalysisService =
                statisticalAnalysisService;
        }


        // =====================================================
        // GET RAW RESULTS
        //
        // GET:
        // /api/experiments/e1/batch/{batchId}
        // =====================================================

        [HttpGet(
            "batch/{batchId:guid}")]

        public async Task<IActionResult>
            GetBatch(
                Guid batchId)
        {
            var results =
                await _context
                    .MAPAValidationResults
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.BatchId
                            ==
                            batchId)
                    .OrderBy(
                        x =>
                            x.ValidationResultId)
                    .ToListAsync();


            if (
                results.Count == 0)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No E1-A results found.",

                        batchId
                    });
            }


            return Ok(
                results);
        }


        // =====================================================
        // GET STATISTICAL SUMMARY
        //
        // GET:
        // /api/experiments/e1/batch/{batchId}/summary
        //
        // Uses the existing:
        //
        // E1ExperimentAnalysisService.GetSummary()
        // =====================================================
        [HttpGet("batch/{batchId:guid}/summary")]
        public async Task<IActionResult> GetSummary(Guid batchId)
        {
            try
            {
                var summary =
                    await _statisticalAnalysisService
                        .AnalyzeBatch(batchId);

                return Ok(summary);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new
                {
                    message = ex.Message,
                    batchId
                });
            }
        }

        // =====================================================
        // EXPORT RAW EXPERIMENT DATA TO CSV
        //
        // GET:
        // /api/experiments/e1/batch/{batchId}/export
        // =====================================================

        [HttpGet(
            "batch/{batchId:guid}/export")]

        public async Task<IActionResult>
            Export(
                Guid batchId)
        {
            var results =
                await _context
                    .MAPAValidationResults
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.BatchId
                            ==
                            batchId)
                    .OrderBy(
                        x =>
                            x.ValidationResultId)
                    .ToListAsync();


            if (
                results.Count == 0)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No E1-A results found.",

                        batchId
                    });
            }


            var csv =
                new StringBuilder();


            // =================================================
            // HEADER
            // =================================================

            csv.AppendLine(
                string.Join(
                    ",",
                    new[]
                    {
                        "BatchId",

                        "ExperimentCode",

                        "StudyId",

                        "ExternalPatientCode",

                        "SourceFileName",


                        // =====================================
                        // DAY SYSTOLIC
                        // =====================================

                        "OriginalDaySystolicMean",

                        "BackendDaySystolicMean",

                        "DaySystolicAbsoluteError",

                        "DaySystolicRelativeError",


                        // =====================================
                        // DAY DIASTOLIC
                        // =====================================

                        "OriginalDayDiastolicMean",

                        "BackendDayDiastolicMean",

                        "DayDiastolicAbsoluteError",

                        "DayDiastolicRelativeError",


                        // =====================================
                        // NIGHT SYSTOLIC
                        // =====================================

                        "OriginalNightSystolicMean",

                        "BackendNightSystolicMean",

                        "NightSystolicAbsoluteError",

                        "NightSystolicRelativeError",


                        // =====================================
                        // NIGHT DIASTOLIC
                        // =====================================

                        "OriginalNightDiastolicMean",

                        "BackendNightDiastolicMean",

                        "NightDiastolicAbsoluteError",

                        "NightDiastolicRelativeError",


                        // =====================================
                        // 24H MEAN PULSE
                        // =====================================

                        "OriginalMeanPulse",

                        "BackendMeanPulse",

                        "MeanPulseAbsoluteError",

                        "MeanPulseRelativeError",


                        // =====================================
                        // PRESSURE LOAD
                        // =====================================

                        "OriginalDayPressureLoad",

                        "BackendDayPressureLoad",

                        "DayPressureLoadAbsoluteError",


                        "OriginalNightPressureLoad",

                        "BackendNightPressureLoad",

                        "NightPressureLoadAbsoluteError",


                        "CreatedAt"
                    }));


            // =================================================
            // DATA
            // =================================================

            foreach (
                var x
                in results)
            {
                csv.AppendLine(
                    string.Join(
                        ",",
                        new[]
                        {
                            Csv(
                                x.BatchId
                                    .ToString()),


                            Csv(
                                x.ExperimentCode),


                            Csv(
                                x.StudyId
                                    .ToString()),


                            Csv(
                                x.ExternalPatientCode),


                            Csv(
                                x.SourceFileName),


                            // =================================
                            // DAY SYSTOLIC
                            // =================================

                            Number(
                                x.OriginalDaySystolicMean),

                            Number(
                                x.BackendDaySystolicMean),

                            Number(
                                x.DaySystolicAbsoluteError),

                            Number(
                                x.DaySystolicRelativeError),


                            // =================================
                            // DAY DIASTOLIC
                            // =================================

                            Number(
                                x.OriginalDayDiastolicMean),

                            Number(
                                x.BackendDayDiastolicMean),

                            Number(
                                x.DayDiastolicAbsoluteError),

                            Number(
                                x.DayDiastolicRelativeError),


                            // =================================
                            // NIGHT SYSTOLIC
                            // =================================

                            Number(
                                x.OriginalNightSystolicMean),

                            Number(
                                x.BackendNightSystolicMean),

                            Number(
                                x.NightSystolicAbsoluteError),

                            Number(
                                x.NightSystolicRelativeError),


                            // =================================
                            // NIGHT DIASTOLIC
                            // =================================

                            Number(
                                x.OriginalNightDiastolicMean),

                            Number(
                                x.BackendNightDiastolicMean),

                            Number(
                                x.NightDiastolicAbsoluteError),

                            Number(
                                x.NightDiastolicRelativeError),


                            // =================================
                            // 24H MEAN PULSE
                            // =================================

                            Number(
                                x.OriginalMeanPulse),

                            Number(
                                x.BackendMeanPulse),

                            Number(
                                x.MeanPulseAbsoluteError),

                            Number(
                                x.MeanPulseRelativeError),


                            // =================================
                            // DAY PRESSURE LOAD
                            // =================================

                            Number(
                                x.OriginalDayPressureLoad),

                            Number(
                                x.BackendDayPressureLoad),

                            Number(
                                x.DayPressureLoadAbsoluteError),


                            // =================================
                            // NIGHT PRESSURE LOAD
                            // =================================

                            Number(
                                x.OriginalNightPressureLoad),

                            Number(
                                x.BackendNightPressureLoad),

                            Number(
                                x.NightPressureLoadAbsoluteError),


                            // =================================
                            // DATE
                            // =================================

                            Csv(
                                x.CreatedAt
                                    .ToString(
                                        "O",

                                        CultureInfo
                                            .InvariantCulture))
                        }));
            }


            byte[] bytes =
                Encoding
                    .UTF8
                    .GetBytes(
                        csv.ToString());


            string fileName =
                $"E1A_{batchId}.csv";


            return File(
                bytes,

                "text/csv",

                fileName);
        }


        // =====================================================
        // GET LARGEST DISCREPANCIES
        //
        // GET:
        // /api/experiments/e1/batch/{batchId}/outliers
        //
        // Only final E1 computational variables are used.
        //
        // ValidReadings is deliberately excluded.
        // =====================================================

        [HttpGet(
            "batch/{batchId:guid}/outliers")]

        public async Task<IActionResult>
            GetOutliers(
                Guid batchId)
        {
            var results =
                await _context
                    .MAPAValidationResults
                    .AsNoTracking()
                    .Where(
                        x =>
                            x.BatchId
                            ==
                            batchId)
                    .ToListAsync();


            if (
                results.Count == 0)
            {
                return NotFound(
                    new
                    {
                        message =
                            "No E1-A results found.",

                        batchId
                    });
            }


            var outliers =
                results

                    .Select(
                        x =>
                            new
                            {
                                // =============================
                                // IDENTIFICATION
                                // =============================

                                x.StudyId,

                                x.ExternalPatientCode,

                                x.SourceFileName,


                                // =============================
                                // DAY SYSTOLIC
                                // =============================

                                DaySystolic =
                                    new
                                    {
                                        Original =
                                            x.OriginalDaySystolicMean,

                                        Backend =
                                            x.BackendDaySystolicMean,

                                        Error =
                                            x.DaySystolicAbsoluteError
                                    },


                                // =============================
                                // DAY DIASTOLIC
                                // =============================

                                DayDiastolic =
                                    new
                                    {
                                        Original =
                                            x.OriginalDayDiastolicMean,

                                        Backend =
                                            x.BackendDayDiastolicMean,

                                        Error =
                                            x.DayDiastolicAbsoluteError
                                    },


                                // =============================
                                // NIGHT SYSTOLIC
                                // =============================

                                NightSystolic =
                                    new
                                    {
                                        Original =
                                            x.OriginalNightSystolicMean,

                                        Backend =
                                            x.BackendNightSystolicMean,

                                        Error =
                                            x.NightSystolicAbsoluteError
                                    },


                                // =============================
                                // NIGHT DIASTOLIC
                                // =============================

                                NightDiastolic =
                                    new
                                    {
                                        Original =
                                            x.OriginalNightDiastolicMean,

                                        Backend =
                                            x.BackendNightDiastolicMean,

                                        Error =
                                            x.NightDiastolicAbsoluteError
                                    },


                                // =============================
                                // 24H MEAN PULSE
                                // =============================

                                MeanPulse =
                                    new
                                    {
                                        Original =
                                            x.OriginalMeanPulse,

                                        Backend =
                                            x.BackendMeanPulse,

                                        Error =
                                            x.MeanPulseAbsoluteError
                                    },


                                // =============================
                                // MAXIMUM ERROR
                                // =============================

                                MaximumContinuousError =

                                    new double?[]
                                    {
                                        x.DaySystolicAbsoluteError,

                                        x.DayDiastolicAbsoluteError,

                                        x.NightSystolicAbsoluteError,

                                        x.NightDiastolicAbsoluteError,

                                        x.MeanPulseAbsoluteError
                                    }

                                    .Where(
                                        value =>
                                            value.HasValue)

                                    .Select(
                                        value =>
                                            value!.Value)

                                    .DefaultIfEmpty(
                                        0)

                                    .Max()
                            })

                    .OrderByDescending(
                        x =>
                            x.MaximumContinuousError)

                    .ToList();


            return Ok(
                outliers);
        }

        // =====================================================
        // FINAL STATISTICAL ANALYSIS
        //
        // GET:
        // /api/experiments/e1/batch/{batchId}/statistics
        // =====================================================

        [HttpGet(
            "batch/{batchId:guid}/statistics")]

        public async Task<IActionResult>
            GetStatistics(
                Guid batchId)
        {
            try
            {
                var result =
                    await _statisticalAnalysisService
                        .AnalyzeBatch(
                            batchId);


                return Ok(
                    result);
            }

            catch (
                InvalidOperationException ex)
            {
                return NotFound(
                    new
                    {
                        message =
                            ex.Message,

                        batchId
                    });
            }

            catch (
                Exception ex)
            {
                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        message =
                            "E1 statistical analysis failed.",

                        detail =
                            ex.Message,

                        inner =
                            ex.InnerException
                                ?.Message
                    });
            }
        }


        // =====================================================
        // CSV HELPERS
        // =====================================================

        private static string
            Csv(
                string? value)
        {
            if (
                string.IsNullOrEmpty(
                    value))
            {
                return "";
            }


            string escaped =
                value.Replace(
                    "\"",

                    "\"\"");


            return
                $"\"{escaped}\"";
        }


        private static string
            Number(
                double? value)
        {
            return value
                ?.ToString(
                    "G17",

                    CultureInfo
                        .InvariantCulture)

                ??

                "";
        }


        private static string
            Number(
                int? value)
        {
            return value
                ?.ToString(
                    CultureInfo
                        .InvariantCulture)

                ??

                "";
        }


        private static string
            Number(
                int value)
        {
            return value
                .ToString(
                    CultureInfo
                        .InvariantCulture);
        }
    }
}
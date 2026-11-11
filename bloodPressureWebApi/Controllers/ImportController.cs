using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class ImportController : ControllerBase
    {
        private readonly MAPAImportService _importService;

        private readonly FeatureExtractionService _featureService;

        private readonly AIDiagnosisService _diagnosisService;

        private readonly ApplicationDbContext _context;

        private readonly DiagnosisQueue _queue;

        public ImportController(
            MAPAImportService importService,
            FeatureExtractionService featureService,
            AIDiagnosisService diagnosisService,
            ApplicationDbContext context, DiagnosisQueue queue)
        {
            _importService =
                importService;

            _featureService =
                featureService;

            _diagnosisService =
                diagnosisService;

            _context =
                context;

            _queue =
                queue;
        }

        [HttpPost("preview/{patientId}")]
        public async Task<IActionResult>
    PreviewImport(
        int patientId,
        IFormFile file)
        {
            try
            {
                var result =
                    await _importService
                        .PreviewImport(
                            file,
                            patientId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error = ex.Message
                });
            }
        }

        [HttpPost("upload/{studyId}")]
        public async Task<IActionResult> UploadExcel(
            int studyId,
            IFormFile file)
        {
            using var transaction =
                await _context
                    .Database
                    .BeginTransactionAsync();

            try
            {
                if (
                    file == null
                    ||
                    file.Length == 0)
                {
                    return BadRequest(new
                    {
                        error =
                            "No file uploaded"
                    });
                }

                var loads =
                    await _importService
                        .ImportStudyFile(
                            file,
                            studyId);

                var study =
                    await _context
                        .MAPAStudies
                        .FirstOrDefaultAsync(
                            x =>
                                x.StudyId ==
                                studyId);

                                if (study != null)
                                {
                                    study.ProcessingStatus =
                                        StudyProcessingStatus
                                        .Uploaded;

                                    await _context
                                        .SaveChangesAsync();
                                }

                await _featureService
                    .ExtractFeatures(
                        studyId,
                        loads.DayPressureLoad,
                        loads.NightPressureLoad);

                //await _diagnosisService
                //    .RunDiagnosis(
                //        studyId);

                BackgroundJob.Enqueue<
                    AIDiagnosisService>(
                        x =>
                            x.RunDiagnosis(
                                studyId));

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,

                    studyId =
                        studyId,

                    file =
                        file.FileName,

                    message =
                        "Study imported successfully"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    error =
                        ex.Message,

                    inner =
                        ex.InnerException?.Message,

                    stack =
                        ex.StackTrace
                });
            }
        }
    }
}

//using Microsoft.AspNetCore.Mvc;
//using bloodPressureWebApi.Services;

//namespace bloodPressureWebApi.Controllers
//{
//    [ApiController]

//    [Route("api/[controller]")]
//    public class ImportController : ControllerBase
//    {
//        private readonly MAPAImportService _importService;

//        private readonly FeatureExtractionService _featureService;

//        private readonly AIDiagnosisService _diagnosisService;

//        public ImportController(
//            MAPAImportService importService,
//            FeatureExtractionService featureService,
//            AIDiagnosisService diagnosisService)
//        {
//            _importService =
//                importService;

//            _featureService =
//                featureService;

//            _diagnosisService =
//                diagnosisService;
//        }

//        // =====================================================
//        // IMPORT SINGLE STUDY
//        // =====================================================

//        [HttpPost("upload/{studyId}")]
//        public async Task<IActionResult> UploadExcel(
//            int studyId,
//            IFormFile file)
//        {
//            try
//            {
//                // =============================================
//                // VALIDATION
//                // =============================================

//                if (
//                    file == null
//                    ||
//                    file.Length == 0)
//                {
//                    return BadRequest(new
//                    {
//                        error =
//                            "No file uploaded"
//                    });
//                }

//                // =============================================
//                // IMPORT RAW READINGS
//                // =============================================

//                await _importService
//                    .ImportStudyFile(
//                        file,
//                        studyId);

//                // =============================================
//                // FEATURE EXTRACTION
//                // =============================================

//                await _featureService
//                    .ExtractFeatures(
//                        studyId);

//                // =============================================
//                // AI DIAGNOSIS
//                // =============================================

//                await _diagnosisService
//                    .RunDiagnosis(
//                        studyId);

//                // =============================================
//                // RESPONSE
//                // =============================================

//                return Ok(new
//                {
//                    success = true,

//                    studyId =
//                        studyId,

//                    file =
//                        file.FileName,

//                    message =
//                        "Study imported successfully"
//                });
//            }
//            catch (Exception ex)
//            {
//                return BadRequest(new
//                {
//                    error =
//                        ex.Message,

//                    inner =
//                        ex.InnerException?.Message,

//                    stack =
//                        ex.StackTrace
//                });
//            }
//        }
//    }
//}
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]

    [Route("api/[controller]")]
    public class BulkImportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private readonly MAPAImportService _importService;

        private readonly FeatureExtractionService _featureService;

        private readonly AIDiagnosisService _diagnosisService;

        public BulkImportController(
            ApplicationDbContext context,
            MAPAImportService importService,
            FeatureExtractionService featureService,
            AIDiagnosisService diagnosisService)
        {
            _context = context;

            _importService = importService;

            _featureService = featureService;

            _diagnosisService = diagnosisService;
        }

        // =====================================================
        // IMPORT FULL FOLDER
        // =====================================================

        [HttpPost("folder/{patientId}")]
        public async Task<IActionResult> ImportFolder(
            int patientId)
        {
            try
            {
                string folder =
                    @"C:\MAPA";

                if (!Directory.Exists(folder))
                {
                    return BadRequest(
                        "Folder not found");
                }

                var files =
                    Directory.GetFiles(
                        folder,
                        "*.xlsx");

                int imported = 0;

                foreach (var filePath in files)
                {
                    // =====================================
                    // CREATE STUDY
                    // =====================================

                    MAPAStudy study =
                        new MAPAStudy
                        {
                            PatientId =
                                patientId,

                            StudyDate =
                                DateTime.Now,

                            CreatedAt =
                                DateTime.Now
                        };

                    _context.MAPAStudies
                        .Add(study);

                    await _context
                        .SaveChangesAsync();

                    // =====================================
                    // FILE
                    // =====================================

                    byte[] bytes =
                        await System.IO.File
                        .ReadAllBytesAsync(
                            filePath);

                    using var stream =
                        new MemoryStream(bytes);

                    IFormFile formFile =
                        new FormFile(
                            stream,
                            0,
                            stream.Length,
                            null,
                            Path.GetFileName(
                                filePath))
                        {
                            Headers =
                                new HeaderDictionary(),

                            ContentType =
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        };

                    // =====================================
                    // IMPORT
                    // =====================================

                    var loads =
                        await _importService
                            .ImportStudyFile(
                                formFile,
                                study.StudyId);

                    await _featureService
                        .ExtractFeatures(
                            study.StudyId,
                            loads.DayPressureLoad,
                            loads.NightPressureLoad);

                    // =====================================
                    // AI
                    // =====================================

                    await _diagnosisService
                        .RunDiagnosis(
                            study.StudyId);

                    imported++;
                }

                return Ok(new
                {
                    success = true,

                    imported =
                        imported
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    error =
                        ex.Message,

                    stack =
                        ex.StackTrace
                });
            }
        }
    }
}
using bloodPressureWebApi.Data;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class ReadingsController
        : ControllerBase
    {
        private readonly
            ApplicationDbContext
            _context;

        public ReadingsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================
        // GET READINGS BY STUDY
        // =====================================

        [HttpGet("study/{studyId}")]
        public async Task<IActionResult>
            GetReadingsByStudy(
                int studyId)
        {
            var readings =
                await _context
                    .MAPAReadings

                    .AsNoTracking()

                    .Where(x =>
                        x.StudyId ==
                        studyId)

                    .OrderBy(x =>
                        x.ReadingDateTime)

                    .Select(x => new
                    {
                        systolic =
                            x.Systolic,

                        diastolic =
                            x.Diastolic,

                        pulse =
                            x.Pulse,

                        time =
                            x.ReadingDateTime
                                .ToString("HH:mm")
                    })

                    .ToListAsync();

            return Ok(readings);
        }
    }
}
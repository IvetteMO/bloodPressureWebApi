using bloodPressureWebApi.Data;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]

    [Route("api/[controller]")]
    public class FeaturesController
        : ControllerBase
    {
        private readonly
            ApplicationDbContext
            _context;

        public FeaturesController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================
        // GET FEATURES BY STUDY
        // =====================================

        [HttpGet("study/{studyId}")]
        public async Task<IActionResult>
            GetFeaturesByStudy(
                int studyId)
        {
            var features =
                await _context
                    .MAPAFeatures

                    .AsNoTracking()

                    .FirstOrDefaultAsync(x =>
                        x.StudyId == studyId);

            if (features == null)
            {
                return NotFound(new
                {
                    success = false,

                    message =
                        "Features not found"
                });
            }

            return Ok(features);
        }
    }
}
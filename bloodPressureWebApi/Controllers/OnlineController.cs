using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]
    [Route("api/health")]
        public class OnlineController : ControllerBase
        {
            [HttpGet]
            public IActionResult Get()
            {
                return Ok(new
                {
                    status = "online",
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }


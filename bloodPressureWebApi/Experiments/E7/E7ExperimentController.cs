using bloodPressureWebApi.Experiments.E7;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers
{
    [ApiController]
    [Route("api/experiments/e7")]
    public class E7ExperimentController : ControllerBase
    {
        private readonly E7ExperimentRunner _runner;

        public E7ExperimentController(
            E7ExperimentRunner runner)
        {
            _runner = runner;
        }

        /// <summary>
        /// Executes Experiment E7:
        /// Reproducibility and Traceability.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(
            typeof(E7ExperimentResultDto),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<E7ExperimentResultDto>> Run(
            [FromBody] E7ExperimentRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            try
            {
                var result =
                    await _runner.RunAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    ex.Message);
            }
        }
    }
}
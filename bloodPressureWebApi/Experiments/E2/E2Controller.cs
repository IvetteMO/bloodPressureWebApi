using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Experiments.E2
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e2")]
    public class E2ExperimentController : ControllerBase
    {
        private readonly E2ExperimentRunner _runner;

        public E2ExperimentController(
            E2ExperimentRunner runner)
        {
            _runner = runner;
        }

        /// <summary>
        /// Experiment E2:
        /// Component-level performance profiling.
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<E2ExperimentResultDto>> Run(
            [FromBody] E2ExperimentRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            E2ExperimentResultDto result =
                await _runner.RunAsync(request);

            return Ok(result);
        }
    }
}
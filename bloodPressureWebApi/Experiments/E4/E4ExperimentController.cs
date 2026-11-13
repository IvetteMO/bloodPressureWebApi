using bloodPressureWebApi.Experiments.E4;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers.Experiments
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e4")]
    public class E4ExperimentController : ControllerBase
    {
        private readonly E4ExperimentRunner _runner;

        public E4ExperimentController(
            E4ExperimentRunner runner)
        {
            _runner = runner;
        }

        /// <summary>
        /// Experiment E4:
        /// Synchronous vs Asynchronous pipeline comparison.
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<E4ExperimentResultDto>> Run(
            [FromBody] E4ExperimentRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            if (request.Iterations <= 0)
            {
                return BadRequest(
                    "Iterations must be greater than zero.");
            }

            var result =
                await _runner.RunAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
    }
}
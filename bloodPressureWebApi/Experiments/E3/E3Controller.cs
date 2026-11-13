using bloodPressureWebApi.Experiments.E3;

using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers.Experiments
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e3")]
    public class E3Controller : ControllerBase
    {
        private readonly E3ExperimentRunner _runner;

        public E3Controller(
            E3ExperimentRunner runner)
        {
            _runner = runner;
        }

        // =====================================================
        // RUN EXPERIMENT
        // =====================================================

        /// <summary>
        /// Executes Experiment E3:
        /// Concurrent diagnosis benchmark.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(E3ExperimentResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<E3ExperimentResultDto>> Run(
            [FromBody] E3ExperimentRequestDto request)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null.");
            }

            bool hasLevels =
                request.ConcurrencyLevels != null &&
                request.ConcurrencyLevels.Count > 0;

            if (hasLevels && request.ConcurrencyLevels!.Any(x => x <= 0))
            {
                return BadRequest(
                    "All concurrency levels must be greater than zero.");
            }

            if (!hasLevels && request.Concurrency <= 0)
            {
                return BadRequest(
                    "Concurrency must be greater than zero.");
            }

            if (request.Iterations <= 0)
            {
                return BadRequest(
                    "Iterations must be greater than zero.");
            }

            try
            {
                E3ExperimentResultDto result =
                    await _runner.RunAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        Message = "Experiment E3 failed.",
                        Error = ex.Message,
                        Detail = ex.ToString()
                    });
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Experiments.E5
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e5")]
    public class E5ExperimentController : ControllerBase
    {
        private readonly E5ExperimentRunner _runner;

        public E5ExperimentController(
            E5ExperimentRunner runner)
        {
            _runner = runner;
        }

        [HttpPost("run")]
        public async Task<ActionResult<E5ExperimentResultDto>> Run(
            [FromBody] E5ExperimentRequestDto request)
        {
            var result = await _runner.RunAsync(request);

            return Ok(result);
        }
    }
}
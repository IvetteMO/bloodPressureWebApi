using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Experiments.E6
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e6")]
    public class E6ExperimentController : ControllerBase
    {
        private readonly E6ExperimentRunner _runner;

        public E6ExperimentController(
            E6ExperimentRunner runner)
        {
            _runner = runner;
        }

        [HttpPost("run")]
        public async Task<ActionResult<E6ExperimentResultDto>> Run(
            [FromBody] E6ExperimentRequestDto request)
        {
            E6ExperimentResultDto result =
                await _runner.RunAsync(request);

            return Ok(result);
        }
    }
}
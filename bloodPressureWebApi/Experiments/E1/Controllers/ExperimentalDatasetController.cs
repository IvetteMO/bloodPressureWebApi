using bloodPressureWebApi.DTOs.Experiments;
using bloodPressureWebApi.Services;

using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Controllers
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]

    [Route("api/[controller]")]

    public class ExperimentalDatasetController
        : ControllerBase
    {
        private readonly
            E1ExperimentRunner
            _datasetService;


        public ExperimentalDatasetController(
            E1ExperimentRunner
                datasetService)
        {
            _datasetService =
                datasetService;

        }


        // =====================================================
        // IMPORT + E1-A VALIDATION
        // =====================================================

        [HttpPost("e1/import-folder")]

        public async Task<IActionResult>
            ImportE1Dataset(
                [FromBody]
                ExperimentalImportRequestDto
                    request)
        {
            try
            {
                var result =
                    await _datasetService
                    .ImportDataset(
                        request);

                return Ok(result);
            }

            catch (
                DirectoryNotFoundException ex)
            {
                return NotFound(
                    new
                    {
                        error =
                            ex.Message
                    });
            }

            catch (
                ArgumentException ex)
            {
                return BadRequest(
                    new
                    {
                        error =
                            ex.Message
                    });
            }

            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        error =
                            ex.Message,

                        inner =
                            ex.InnerException
                                ?.Message
                    });
            }
        }
    }
}
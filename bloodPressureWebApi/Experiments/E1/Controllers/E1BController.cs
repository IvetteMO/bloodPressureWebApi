using bloodPressureWebApi.Data;
using bloodPressureWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Controllers
{
    // Solo se publica con Experiments:Enabled = true (ver Program.cs)
    [bloodPressureWebApi.Infrastructure.Experiments.ExperimentEndpoint]
    [ApiController]
    [Route("api/experiments/e1b")]
    public class E1BController : ControllerBase
    {
        private readonly E1BExperimentRunner _runner;
        private readonly ApplicationDbContext _context;

        public E1BController(
            E1BExperimentRunner runner,
            ApplicationDbContext context)
        {
            _runner = runner;
            _context = context;
        }

        //====================================================
        // RUN E1-B
        // POST: api/experiments/e1b/run
        //====================================================

        [HttpPost("run")]
        public async Task<IActionResult> Run()
        {
            try
            {
                var result = await _runner.RunAsync();

                return Ok(result);
            }
            catch (FileNotFoundException ex)
            {
                return BadRequest(new
                {
                    Message = "The MATLAB reference file was not found.",
                    Detail = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new
            //    {
            //        Message = "An error occurred while executing E1-B.",
            //        Detail = ex.Message
            //    });

            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                return StatusCode(500, new
                {
                    message = "An error occurred while executing E1-B.",
                    detail = ex.ToString()
                });
            }
        }


        //====================================================
        // GET RESULTS
        // GET: api/experiments/e1b/results
        //====================================================

        [HttpGet("results")]
        public IActionResult GetResults()
        {
            string path = Path.Combine(
                @"C:\MAPA_E1_TEST",
                "ExperimentResults",
                "E1B_ValidationResults.csv");

            if (!System.IO.File.Exists(path))
            {
                return NotFound(new
                {
                    message = "E1-B results CSV has not been generated yet."
                });
            }

            return PhysicalFile(
                path,
                "text/csv",
                "E1B_ValidationResults.csv");
        }

    }
}


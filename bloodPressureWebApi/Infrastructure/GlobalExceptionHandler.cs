using bloodPressureWebApi.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Infrastructure
{
    /// <summary>
    /// Convierte cualquier excepción no atrapada en ProblemDetails (RFC 7807).
    ///
    /// - IUserFacingException -> 400 con el mensaje (escrito para el usuario).
    /// - Cliente canceló la petición -> 499, sin registrarlo como error.
    /// - Cualquier otra -> 500 con un mensaje genérico. El detalle completo
    ///   va solo al log, junto con el traceId que también recibe el
    ///   frontend: con ese id se encuentra el error en el log.
    ///   En Development se agrega el mensaje real para depurar más rápido.
    /// </summary>
    public sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        private const int ClientClosedRequest = 499;

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException
                && httpContext.RequestAborted.IsCancellationRequested)
            {
                logger.LogInformation(
                    "Request {Method} {Path} cancelled by the client.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                httpContext.Response.StatusCode = ClientClosedRequest;
                return true;
            }

            var problem = exception switch
            {
                IUserFacingException userFacing => new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = userFacing.Title,
                    Detail = exception.Message
                },

                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocurrió un error interno",
                    Detail = environment.IsDevelopment()
                        ? exception.Message
                        : "Intenta de nuevo. Si el problema continúa, comparte el código de seguimiento con soporte."
                }
            };

            if (problem.Status >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(
                    exception,
                    "Unhandled exception on {Method} {Path}",
                    httpContext.Request.Method,
                    httpContext.Request.Path);
            }
            else
            {
                logger.LogWarning(
                    "Rejected {Method} {Path}: {Message}",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    exception.Message);
            }

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await problemDetailsService.TryWriteAsync(
                new ProblemDetailsContext
                {
                    HttpContext = httpContext,
                    ProblemDetails = problem,
                    Exception = exception
                });
        }
    }
}

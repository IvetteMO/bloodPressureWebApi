using System.Security.Claims;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.Exceptions;
using bloodPressureWebApi.Infrastructure.Experiments;
using bloodPressureWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Auditoría: quién consultó o modificó qué, y cuándo.
    ///
    /// Se registra:
    ///   - toda petición que modifica datos (POST/PUT/PATCH/DELETE)
    ///   - toda consulta de un registro concreto (GET con id en la ruta),
    ///     por ejemplo abrir el MAPA de un paciente
    /// No se registran listados, el dashboard ni el polling de estado.
    ///
    /// Se guarda con un DbContext propio: si la petición falló y deshizo su
    /// transacción, el registro de auditoría se guarda de todos modos.
    /// Un error al auditar nunca rompe la petición.
    /// </summary>
    public sealed class AuditActionFilter(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditActionFilter> logger) : IAsyncActionFilter
    {
        private static readonly string[] IdRouteKeys =
        {
            "id", "studyId", "patientId", "riskFactorId"
        };

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var executed = await next();

            if (!ShouldAudit(context))
            {
                return;
            }

            try
            {
                var http = context.HttpContext;
                var user = http.User;

                var descriptor = (ControllerActionDescriptor)context.ActionDescriptor;

                var statusCode = executed.Exception is not null && !executed.ExceptionHandled
                    ? (executed.Exception is IUserFacingException
                        ? StatusCodes.Status400BadRequest
                        : StatusCodes.Status500InternalServerError)
                    : (executed.Result as IStatusCodeActionResult)?.StatusCode
                      ?? http.Response.StatusCode;

                using var scope = scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                db.AuditLogs.Add(new AuditLog
                {
                    Action = $"{descriptor.ControllerName}.{descriptor.ActionName}",
                    EntityName = descriptor.ControllerName,
                    EntityId = FindEntityId(context),
                    UserId = user.GetUserId(),
                    UserEmail = user.FindFirstValue(ClaimTypes.Email),
                    UserRole = user.FindFirstValue(ClaimTypes.Role),
                    HttpMethod = http.Request.Method,
                    Path = http.Request.Path.ToString(),
                    StatusCode = statusCode,
                    IpAddress = http.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.Now
                });

                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not write audit entry for {Path}.", context.HttpContext.Request.Path);
            }
        }

        private static bool ShouldAudit(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            {
                return false;
            }

            // Experimentos: generan miles de llamadas automáticas.
            if (descriptor.ControllerTypeInfo.IsDefined(typeof(ExperimentEndpointAttribute), inherit: true))
            {
                return false;
            }

            // Auth audita sus propios eventos (login, logout...).
            if (descriptor.ControllerName == "Auth")
            {
                return false;
            }

            // Polling del estado del diagnóstico: cada 2 s, sin datos clínicos.
            if (descriptor.ActionName == "GetProcessingStatus")
            {
                return false;
            }

            var method = context.HttpContext.Request.Method;

            return !HttpMethods.IsGet(method) || FindEntityId(context) != 0;
        }

        private static int FindEntityId(ActionExecutingContext context)
        {
            foreach (var key in IdRouteKeys)
            {
                if (context.RouteData.Values.TryGetValue(key, out var value)
                    && int.TryParse(value?.ToString(), out var id))
                {
                    return id;
                }
            }

            return 0;
        }
    }
}

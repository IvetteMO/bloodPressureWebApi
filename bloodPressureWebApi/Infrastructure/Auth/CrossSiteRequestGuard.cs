namespace bloodPressureWebApi.Infrastructure.Auth
{
    /// <summary>
    /// Protección CSRF para la sesión con cookie.
    ///
    /// Los navegadores modernos envían Sec-Fetch-Site en cada petición.
    /// Una petición que MODIFICA datos (POST/PUT/PATCH/DELETE) iniciada
    /// desde otro sitio se rechaza, salvo que su Origin esté en
    /// Cors:AllowedOrigins (el frontend legítimo).
    ///
    /// Peticiones sin Sec-Fetch-Site (curl, scripts, Postman) se permiten:
    /// no llevan la cookie de un usuario que esté navegando.
    /// </summary>
    public sealed class CrossSiteRequestGuard(RequestDelegate next, IConfiguration configuration)
    {
        private readonly HashSet<string> _allowedOrigins = new(
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsUnsafe(context.Request.Method)
                && context.Request.Headers.TryGetValue("Sec-Fetch-Site", out var site)
                && site != "same-origin"
                && site != "none"
                && !_allowedOrigins.Contains(context.Request.Headers.Origin.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                await context.Response.WriteAsJsonAsync(new
                {
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    title = "Petición rechazada",
                    status = StatusCodes.Status403Forbidden,
                    detail = "Las peticiones que modifican datos deben venir de la aplicación."
                });

                return;
            }

            await next(context);
        }

        private static bool IsUnsafe(string method) =>
            !(HttpMethods.IsGet(method)
              || HttpMethods.IsHead(method)
              || HttpMethods.IsOptions(method));
    }
}

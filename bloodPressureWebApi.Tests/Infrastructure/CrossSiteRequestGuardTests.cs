using bloodPressureWebApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace bloodPressureWebApi.Tests.Infrastructure
{
    /// <summary>Protección CSRF de la sesión con cookie.</summary>
    public class CrossSiteRequestGuardTests
    {
        private const string Frontend = "https://app.softdiagnosis.mx";

        private static async Task<(int Status, bool NextCalled)> Run(
            string method,
            string? fetchSite,
            string? origin = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins:0"] = Frontend
                })
                .Build();

            var nextCalled = false;

            var guard = new CrossSiteRequestGuard(
                _ =>
                {
                    nextCalled = true;
                    return Task.CompletedTask;
                },
                configuration);

            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Response.Body = new MemoryStream();

            if (fetchSite is not null) context.Request.Headers["Sec-Fetch-Site"] = fetchSite;
            if (origin is not null) context.Request.Headers.Origin = origin;

            await guard.InvokeAsync(context);

            return (context.Response.StatusCode, nextCalled);
        }

        [Fact]
        public async Task Blocks_a_cross_site_POST_from_an_unknown_origin()
        {
            var (status, nextCalled) = await Run("POST", "cross-site", "https://malicioso.com");

            Assert.Equal(StatusCodes.Status403Forbidden, status);
            Assert.False(nextCalled);
        }

        [Theory]
        [InlineData("POST", "same-origin", null)]          // proxy de Vite / mismo dominio
        [InlineData("POST", "cross-site", Frontend)]      // frontend en otro dominio permitido
        [InlineData("GET", "cross-site", "https://x.com")] // leer no modifica nada
        [InlineData("DELETE", null, null)]                  // curl, scripts, Postman
        public async Task Lets_legitimate_requests_through(string method, string? fetchSite, string? origin)
        {
            var (_, nextCalled) = await Run(method, fetchSite, origin);

            Assert.True(nextCalled);
        }
    }
}

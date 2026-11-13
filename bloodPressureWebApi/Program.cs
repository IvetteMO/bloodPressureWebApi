using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.E2;
using bloodPressureWebApi.Experiments.E3;
using bloodPressureWebApi.Experiments.E4;
using bloodPressureWebApi.Experiments.E5;
using bloodPressureWebApi.Experiments.E5.FaultInjection;
using bloodPressureWebApi.Experiments.E6;
using bloodPressureWebApi.Experiments.E7;
using bloodPressureWebApi.Infrastructure;
using bloodPressureWebApi.Infrastructure.Experiments;
using bloodPressureWebApi.Infrastructure.Auth;
using bloodPressureWebApi.Infrastructure.Matlab;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;
using bloodPressureWebApi.Services.Interfaces;
using bloodPressureWebApi.Workers;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Text;
using System.Text.Json.Serialization;

// =====================================================
// EPPLUS LICENSE
// =====================================================

ExcelPackage.License
    .SetNonCommercialPersonal(
        "Ivette");


var builder =
    WebApplication.CreateBuilder(args);

// =====================================================
// EXPERIMENTS
// true solo en appsettings.Development.json. En produccion:
// - las rutas /api/experiments/* (y BulkImport) no existen
// - la inyeccion de fallos de E5 no se puede activar
// - la traza E1-B no escribe lecturas de pacientes a disco
// =====================================================

var experimentsEnabled =
    builder.Configuration.GetValue("Experiments:Enabled", false);

// =====================================================
// DATABASE
// =====================================================

builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection")));

// =====================================================
// SERVICES
// =====================================================

builder.Services
    .AddSingleton<MatlabService>();

// =====================================================
// MATLAB WARMUP + HEALTH CHECKS
// Matlab:WarmupOnStartup = true  -> el Runtime se inicializa al arrancar
// Matlab:WarmupOnStartup = false -> en la primera peticion (como en el
//                                   articulo; asi E2/E3 miden el arranque
//                                   en frio igual que antes)
// =====================================================

var matlabWarmupOnStartup =
    builder.Configuration.GetValue("Matlab:WarmupOnStartup", !experimentsEnabled);

builder.Services
    .AddSingleton<MatlabReadiness>();

if (matlabWarmupOnStartup)
{
    builder.Services
        .AddHostedService<MatlabWarmupService>();
}

builder.Services
    .AddHealthChecks()
    .AddCheck<MatlabHealthCheck>("matlab", tags: new[] { "ready" });

builder.Services
    .AddScoped<MAPAImportService>();

builder.Services
    .AddScoped<FeatureExtractionService>();

builder.Services
    .AddScoped<AIDiagnosisService>();

builder.Services
    .AddScoped<StudyIntakeService>();

// =====================================================
// AUTH: cookie HttpOnly + roles
// - Todas las rutas piden sesion (FallbackPolicy), salvo las marcadas
//   con [AllowAnonymous]: login, logout, health checks y experimentos.
// - La cookie se valida en cada peticion contra la BD (SessionValidator):
//   desactivar a un usuario o cambiar su rol cierra sus sesiones.
// =====================================================

builder.Services
    .AddScoped<AuthService>();

builder.Services
    .AddScoped<
        IPasswordHasher<bloodPressureWebApi.Models.AppUser>,
        PasswordHasher<bloodPressureWebApi.Models.AppUser>>();

builder.Services
    .AddAuthentication(AuthDefaults.Scheme)
    .AddCookie(AuthDefaults.Scheme, options =>
    {
        options.Cookie.Name = AuthDefaults.CookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        // Lax: frontend y API en el mismo sitio (proxy de Vite en
        // desarrollo, o app.dominio / api.dominio en produccion).
        // None: solo si el frontend queda en OTRO dominio (p. ej. Vercel
        // y Azure con dominios distintos).
        options.Cookie.SameSite =
            builder.Configuration.GetValue("Auth:CookieSameSite", SameSiteMode.Lax);

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Es una API: sin redirecciones a paginas de login.
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },

            OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            },

            OnValidatePrincipal = SessionValidator.ValidateAsync
        };
    });

builder.Services
    .AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

// Login: maximo 10 intentos por minuto por IP (ademas del bloqueo por
// usuario tras 5 fallos).
builder.Services
    .AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(AuthDefaults.LoginRateLimitPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

// Primer arranque: crea el administrador si no hay usuarios.
builder.Services
    .AddHostedService<BootstrapAdminService>();

builder.Services
    .AddScoped<DashboardService>();


builder.Services
    .AddScoped<MAPAMetadataReaderService>();

builder.Services
    .AddScoped<E1ValidationService>();


builder.Services
    .AddScoped<E1ExperimentRunner>();

// E1-B
builder.Services.AddScoped<IE1BReferenceService, E1BReferenceService>();
builder.Services.AddScoped<E1BComparisonService>();
builder.Services.AddScoped<E1BExperimentRunner>();
builder.Services.AddScoped<E1BAnalysisService>();
builder.Services.AddSingleton<E1BBackendTrace>();


builder.Services.AddScoped<E2ExperimentRunner>();

builder.Services.AddScoped<E3ExperimentRunner>();

builder.Services.AddScoped<E4ExperimentRunner>();

builder.Services.AddScoped<E5ExperimentRunner>();

if (experimentsEnabled)
{
    builder.Services.AddSingleton<
        IFaultInjectionService,
        FaultInjectionService>();
}
else
{
    builder.Services.AddSingleton<
        IFaultInjectionService,
        DisabledFaultInjectionService>();
}

builder.Services.AddScoped<E6ExperimentRunner>();

builder.Services.AddScoped<E7ExperimentRunner>();


builder.Services
    .AddSingleton<DiagnosisQueue>();

// NUEVO: numero de consumidores de la cola (por defecto 1 = comportamiento
// original). E4/E5/E6 lo ajustan en tiempo de ejecucion.
builder.Services
    .AddSingleton<DiagnosisWorkerOptions>();

// NUEVO: arnes comun de orquestacion para E4, E5 (modo cola) y E6.
builder.Services
    .AddScoped<OrchestrationHarness>();

builder.Services
    .AddScoped<E1StatisticalAnalysisService>();

builder.Services
    .AddHostedService<DiagnosisWorker>();

// Re-encola los estudios que quedaron a medio recalcular si el backend
// se reinicio (la cola vive en memoria).
builder.Services
    .AddHostedService<DiagnosisRecoveryService>();

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services
    .AddControllers(options =>
    {
        // Auditoria: quien consulto o modifico que.
        options.Filters.Add<AuditActionFilter>();

        // Los experimentos no piden sesion (solo existen en Development).
        options.Conventions.Add(new ExperimentAnonymousConvention());
    })

    .ConfigureApplicationPartManager(manager =>
        manager.FeatureProviders.Add(
            new ExperimentControllerFeatureProvider(experimentsEnabled)))

    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .ReferenceHandler =
                ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions
            .WriteIndented = true;
    });

//builder.Services
//.AddControllers()

//.AddJsonOptions(options =>
//{
//    options.JsonSerializerOptions
//    .ReferenceHandler =
//        ReferenceHandler.IgnoreCycles;
//});

// =====================================================
// SWAGGER
// =====================================================

builder.Services
    .AddEndpointsApiExplorer();

builder.Services
    .AddSwaggerGen();



// =====================================================
// ERRORS (ProblemDetails, RFC 7807)
// Todas las respuestas de error tienen la misma forma:
// { type, title, status, detail, traceId }
// =====================================================

builder.Services
    .AddProblemDetails();

builder.Services
    .AddExceptionHandler<GlobalExceptionHandler>();

// =====================================================
// CORS
// Solo los origenes de Cors:AllowedOrigins (appsettings).
// =====================================================

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services
    .AddCors(options =>
    {
        options.AddPolicy(
            "Frontend",
            policy =>
            {
                policy

                    .WithOrigins(allowedOrigins)

                    .AllowAnyHeader()

                    .AllowAnyMethod()

                    // Necesario si el frontend queda en otro dominio y
                    // envia la cookie de sesion.
                    .AllowCredentials();
            });
    });

Encoding.RegisterProvider(
    CodePagesEncodingProvider.Instance
);

var app = builder.Build();

// Fuerza la inicialización de las trazas al arrancar la API.
try
{
    _ = app.Services.GetRequiredService<E1BBackendTrace>();
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "No se pudo inicializar E1BBackendTrace.");
    throw;
}

// =====================================================
// PIPELINE
// =====================================================

app.UseExceptionHandler();

app.UseStatusCodePages();

if (experimentsEnabled)
{
    app.Logger.LogWarning(
        "EXPERIMENTS ENABLED: /api/experiments/* and fault injection are available. " +
        "Do not use this configuration in production.");
}

if (allowedOrigins.Length == 0)
{
    app.Logger.LogWarning(
        "Cors:AllowedOrigins is empty: browsers will not be able to call this API.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

// CSRF: rechaza peticiones que modifican datos desde otros sitios.
app.UseMiddleware<CrossSiteRequestGuard>();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

// /health/live  -> el proceso responde
// /health/ready -> ademas, MATLAB ya esta listo para diagnosticar
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
}).AllowAnonymous();

app.Run();
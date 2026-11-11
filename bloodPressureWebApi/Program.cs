using bloodPressureWebApi.Data;
using bloodPressureWebApi.Experiments.E2;
using bloodPressureWebApi.Experiments.E3;
using bloodPressureWebApi.Experiments.E4;
using bloodPressureWebApi.Experiments.E5;
using bloodPressureWebApi.Experiments.E5.FaultInjection;
using bloodPressureWebApi.Experiments.E6;
using bloodPressureWebApi.Experiments.E7;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Services;
using bloodPressureWebApi.Services.Interfaces;
using bloodPressureWebApi.Workers;
using Hangfire;
using Hangfire.SqlServer;
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

builder.Services
    .AddScoped<MAPAImportService>();

builder.Services
    .AddScoped<FeatureExtractionService>();

builder.Services
    .AddScoped<AIDiagnosisService>();

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

builder.Services.AddSingleton<
    IFaultInjectionService,
    FaultInjectionService>();

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

builder.Services
    .AddHangfireServer();

// =====================================================
// CONTROLLERS
// =====================================================

builder.Services
    .AddControllers()

    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions
            .ReferenceHandler =
                ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions
            .WriteIndented = true;
    });

    builder.Services
    .AddHangfire(config =>
        config.SetDataCompatibilityLevel(
            CompatibilityLevel.Version_180)

        .UseSimpleAssemblyNameTypeSerializer()

        .UseRecommendedSerializerSettings()

        .UseSqlServerStorage(
            builder.Configuration
            .GetConnectionString("DefaultConnection")));


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
// CORS
// =====================================================

builder.Services
    .AddCors(options =>
    {
        options.AddPolicy(
            "AllowReact",
            policy =>
            {
                policy

                    .AllowAnyOrigin()

                    .AllowAnyHeader()

                    .AllowAnyMethod();
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("AllowReact");

app.UseAuthorization();

app.MapControllers();

app.UseHangfireDashboard();

app.Run();
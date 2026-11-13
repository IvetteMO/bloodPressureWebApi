using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Data;
using bloodPressureWebApi.DTOs.Patients;
using bloodPressureWebApi.DTOs.Studies;
using bloodPressureWebApi.Models;
using bloodPressureWebApi.Queues;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Services
{
    /// <summary>
    /// Alta de estudios "todo o nada".
    ///
    /// Antes el frontend hacía 4 llamadas separadas (paciente, estudio
    /// vacío, factores de riesgo, Excel). Si el Excel fallaba, lo anterior
    /// ya estaba guardado y quedaban pacientes con estudios vacíos.
    ///
    /// Aquí todo lo que valida el archivo corre en UNA transacción
    /// (paciente, estudio, factores, importación y características): si el
    /// Excel es inválido o el estudio está duplicado, no se guarda nada y
    /// la excepción llega al GlobalExceptionHandler con su mensaje.
    ///
    /// El diagnóstico (MATLAB) NO va dentro de la transacción: se encola
    /// después del commit, igual que en el resto de la app. Así no se
    /// mantiene la transacción abierta ~20 s en el arranque en frío, y si
    /// MATLAB falla el estudio queda FAILED con reintentos, en vez de
    /// perder un archivo válido.
    /// </summary>
    public sealed class StudyIntakeService(
        ApplicationDbContext context,
        MAPAImportService importService,
        FeatureExtractionService featureService,
        DiagnosisQueue diagnosisQueue,
        ILogger<StudyIntakeService> logger)
    {
        private static readonly TimeSpan DefaultAwakeTime = new(6, 0, 0);
        private static readonly TimeSpan DefaultSleepTime = new(22, 0, 0);

        // =====================================================
        // PACIENTE NUEVO + PRIMER ESTUDIO
        // =====================================================

        public async Task<StudyIntakeResponse> RegisterPatientAsync(
            RegisterPatientRequest request,
            CancellationToken cancellationToken)
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            var patient = new Patients
            {
                FirstName = request.FirstName?.Trim(),
                LastName = request.LastName?.Trim(),
                BirthDate = request.BirthDate?.Date,
                Sex = request.Sex,
                PhoneNumber = NullIfBlank(request.PhoneNumber),
                Email = NullIfBlank(request.Email),
                Notes = NullIfBlank(request.Notes),
                CreatedAt = DateTime.Now
            };

            context.Patients.Add(patient);

            await context.SaveChangesAsync(cancellationToken);

            var study = await CreateAndProcessStudyAsync(
                patient.PatientId,
                request,
                cancellationToken);

            // Sin token: lo validado se guarda aunque el cliente cierre la pestaña.
            await transaction.CommitAsync(CancellationToken.None);

            await EnqueueDiagnosisAsync(study.StudyId);

            logger.LogInformation(
                "Patient {PatientId} registered with study {StudyId}.",
                patient.PatientId,
                study.StudyId);

            return new StudyIntakeResponse(
                patient.PatientId,
                study.StudyId,
                study.StudyStatus,
                study.ProcessingStatus);
        }

        // =====================================================
        // ESTUDIO NUEVO PARA UN PACIENTE EXISTENTE
        // Regresa null si el paciente no existe.
        // =====================================================

        public async Task<StudyIntakeResponse?> AddStudyAsync(
            CreateStudyWithFileRequest request,
            CancellationToken cancellationToken)
        {
            var patientId = request.PatientId!.Value;

            var patientExists = await context.Patients
                .AnyAsync(x => x.PatientId == patientId, cancellationToken);

            if (!patientExists)
            {
                return null;
            }

            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            var study = await CreateAndProcessStudyAsync(
                patientId,
                request,
                cancellationToken);

            await transaction.CommitAsync(CancellationToken.None);

            await EnqueueDiagnosisAsync(study.StudyId);

            logger.LogInformation(
                "Study {StudyId} added to patient {PatientId}.",
                study.StudyId,
                patientId);

            return new StudyIntakeResponse(
                patientId,
                study.StudyId,
                study.StudyStatus,
                study.ProcessingStatus);
        }

        // =====================================================
        // PASOS COMUNES (dentro de la transacción del llamador)
        // =====================================================

        private async Task<MAPAStudy> CreateAndProcessStudyAsync(
            int patientId,
            StudyIntakeRequest request,
            CancellationToken cancellationToken)
        {
            var now = DateTime.Now;

            var study = new MAPAStudy
            {
                PatientId = patientId,
                AwakeTime = request.AwakeTime ?? DefaultAwakeTime,
                SleepTime = request.SleepTime ?? DefaultSleepTime,
                CustomSchedule = request.AwakeTime is not null && request.SleepTime is not null,
                StudyDate = now,
                StudyStatus = "CREATED",
                ProcessingStatus = StudyProcessingStatus.Processing,
                CreatedAt = now
            };

            context.MAPAStudies.Add(study);

            await context.SaveChangesAsync(cancellationToken);

            var weight = request.Weight!.Value;
            var height = request.Height!.Value;

            context.PatientRiskFactors.Add(new PatientRiskFactor
            {
                StudyId = study.StudyId,
                Weight = weight,
                Height = height,
                BMI = Math.Round(weight / (height * height), 1),
                Smoking = ToFlag(request.Smoking!.Value),
                Diabetes = ToFlag(request.Diabetes!.Value),
                HypertensionTreatment = ToFlag(request.HypertensionTreatment!.Value),
                ParentalHypertension = request.ParentalHypertension!.Value,
                CreatedAt = now
            });

            await context.SaveChangesAsync(cancellationToken);

            // A partir de aqui los servicios no reciben token: son los mismos
            // que usan los experimentos y su firma no se cambia.
            var loads = await importService.ImportStudyFile(
                request.File!,
                study.StudyId);

            // Aqui se valida que haya 47 lecturas y lecturas de dia y noche:
            // si no, InvalidStudyDataException y no se guarda nada.
            await featureService.ExtractFeatures(
                study.StudyId,
                loads.DayPressureLoad,
                loads.NightPressureLoad);

            study.ProcessingStatus = StudyProcessingStatus.Queued;

            await context.SaveChangesAsync(CancellationToken.None);

            return study;
        }

        private async Task EnqueueDiagnosisAsync(int studyId)
        {
            await diagnosisQueue.EnqueueAsync(
                new DiagnosisJob
                {
                    StudyId = studyId
                });
        }

        private static byte ToFlag(bool value) => value ? (byte)1 : (byte)0;

        private static string? NullIfBlank(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

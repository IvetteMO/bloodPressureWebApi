using bloodPressureWebApi.Data;
using bloodPressureWebApi.Models;
using Microsoft.EntityFrameworkCore;

namespace bloodPressureWebApi.Tests.Support
{
    /// <summary>
    /// Base de datos en memoria, nueva para cada prueba (nombre único).
    /// </summary>
    public static class TestDb
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString(),
                    // RowVersion y otras columnas que SQL Server llena solo.
                    inMemory => inMemory.EnableNullChecks(false))
                .Options;

            return new ApplicationDbContext(options);
        }

        public static Patients AddPatient(ApplicationDbContext db, byte? sex = 1, DateTime? birthDate = null)
        {
            var patient = new Patients
            {
                FirstName = "Test",
                LastName = "Patient",
                Sex = sex,
                BirthDate = birthDate ?? new DateTime(1980, 5, 1),
                PhoneNumber = "6640000000",
                Notes = "Seguimiento",
                CreatedAt = DateTime.Now
            };

            db.Patients.Add(patient);
            db.SaveChanges();

            return patient;
        }

        public static MAPAStudy AddStudy(ApplicationDbContext db, int patientId, string? processingStatus = "COMPLETED")
        {
            var study = new MAPAStudy
            {
                PatientId = patientId,
                StudyDate = DateTime.Now,
                AwakeTime = new TimeSpan(6, 0, 0),
                SleepTime = new TimeSpan(22, 0, 0),
                StudyStatus = "ANALYZED",
                ProcessingStatus = processingStatus,
                CreatedAt = DateTime.Now
            };

            db.MAPAStudies.Add(study);
            db.SaveChanges();

            return study;
        }

        public static PatientRiskFactor AddRiskFactors(
            ApplicationDbContext db,
            int studyId,
            DateTime? createdAt = null,
            double weight = 70,
            double height = 1.65)
        {
            var risk = new PatientRiskFactor
            {
                StudyId = studyId,
                Weight = weight,
                Height = height,
                BMI = Math.Round(weight / (height * height), 1),
                Smoking = 0,
                Diabetes = 0,
                HypertensionTreatment = 0,
                ParentalHypertension = 0,
                CreatedAt = createdAt ?? DateTime.Now
            };

            db.PatientRiskFactors.Add(risk);
            db.SaveChanges();

            return risk;
        }
    }
}

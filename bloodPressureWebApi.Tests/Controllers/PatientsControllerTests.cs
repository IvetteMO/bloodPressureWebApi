using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Controllers;
using bloodPressureWebApi.DTOs.Patients;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Tests.Controllers
{
    /// <summary>
    /// Regresión del bug de pacientes: el formulario guardaba mal el sexo y
    /// borraba teléfono y notas. Además, cambiar sexo o edad debe
    /// recalcular los diagnósticos (FHS, CVDRisk y edad del corazón).
    /// </summary>
    public class PatientsControllerTests
    {
        private static PatientRequest RequestFor(byte sex, DateTime birthDate) =>
            new(
                FirstName: "  Ana ",
                LastName: "López",
                BirthDate: birthDate,
                Sex: sex,
                PhoneNumber: "6641234567",
                Email: "",
                Notes: "Seguimiento HTA");

        [Fact]
        public async Task UpdatePatient_saves_every_field_with_the_api_names()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db, sex: 0);

            var controller = new PatientsController(db, new DiagnosisQueue());

            var result = await controller.UpdatePatient(
                patient.PatientId,
                RequestFor(sex: 1, birthDate: new DateTime(1984, 3, 1)),
                CancellationToken.None);

            Assert.IsType<OkObjectResult>(result);

            var saved = db.Patients.Single();
            Assert.Equal("Ana", saved.FirstName);
            Assert.Equal((byte)1, saved.Sex);
            Assert.Equal("6641234567", saved.PhoneNumber);
            Assert.Equal("Seguimiento HTA", saved.Notes);
            Assert.Null(saved.Email);
        }

        [Fact]
        public async Task UpdatePatient_requeues_all_studies_when_sex_changes()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db, sex: 0, birthDate: new DateTime(1984, 3, 1));
            TestDb.AddStudy(db, patient.PatientId);
            TestDb.AddStudy(db, patient.PatientId);

            var queue = new DiagnosisQueue();
            var controller = new PatientsController(db, queue);

            await controller.UpdatePatient(
                patient.PatientId,
                RequestFor(sex: 1, birthDate: new DateTime(1984, 3, 1)),
                CancellationToken.None);

            Assert.All(db.MAPAStudies, study => Assert.Equal(StudyProcessingStatus.Queued, study.ProcessingStatus));
            Assert.Equal(2, queue.PendingCount);
        }

        [Fact]
        public async Task UpdatePatient_does_not_requeue_when_only_contact_data_changes()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db, sex: 1, birthDate: new DateTime(1984, 3, 1));
            TestDb.AddStudy(db, patient.PatientId);

            var queue = new DiagnosisQueue();
            var controller = new PatientsController(db, queue);

            await controller.UpdatePatient(
                patient.PatientId,
                RequestFor(sex: 1, birthDate: new DateTime(1984, 3, 1)),
                CancellationToken.None);

            Assert.Equal(StudyProcessingStatus.Completed, db.MAPAStudies.Single().ProcessingStatus);
            Assert.Equal(0, queue.PendingCount);
        }
    }
}

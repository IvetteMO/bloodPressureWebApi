using bloodPressureWebApi.Constants;
using bloodPressureWebApi.Controllers;
using bloodPressureWebApi.DTOs.RiskFactors;
using bloodPressureWebApi.Queues;
using bloodPressureWebApi.Tests.Support;
using Microsoft.AspNetCore.Mvc;

namespace bloodPressureWebApi.Tests.Controllers
{
    /// <summary>
    /// Regresión del bug del punto 1: el modal mandaba el StudyId como
    /// RiskFactorId y borraba peso, talla e IMC.
    /// </summary>
    public class RiskFactorsControllerTests
    {
        private static readonly UpdateRiskFactorsRequest DiabeticSmoker =
            new(Smoking: true, Diabetes: true, HypertensionTreatment: false, ParentalHypertension: 2);

        [Fact]
        public async Task UpsertByStudy_updates_the_risk_factors_of_that_study_and_keeps_weight_height_and_bmi()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db);
            var study = TestDb.AddStudy(db, patient.PatientId);
            var risk = TestDb.AddRiskFactors(db, study.StudyId, weight: 82, height: 1.70);

            var controller = new RiskFactorsController(db, new DiagnosisQueue());

            var result = await controller.UpsertByStudy(study.StudyId, DiabeticSmoker, CancellationToken.None);

            Assert.IsType<AcceptedAtActionResult>(result);

            var saved = db.PatientRiskFactors.Single(x => x.RiskFactorId == risk.RiskFactorId);
            Assert.Equal((byte)1, saved.Smoking);
            Assert.Equal((byte)1, saved.Diabetes);
            Assert.Equal((byte)2, saved.ParentalHypertension);
            Assert.Equal(82.0, saved.Weight);
            Assert.Equal(1.70, saved.Height);
            Assert.Equal(risk.BMI, saved.BMI);
        }

        [Fact]
        public async Task UpsertByStudy_does_not_touch_another_study_whose_RiskFactorId_equals_the_StudyId()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db);

            // Estudio A con un registro de factores; estudio B sin factores aún.
            var studyA = TestDb.AddStudy(db, patient.PatientId);
            var riskOfA = TestDb.AddRiskFactors(db, studyA.StudyId);
            var studyB = TestDb.AddStudy(db, patient.PatientId);

            var controller = new RiskFactorsController(db, new DiagnosisQueue());

            await controller.UpsertByStudy(studyB.StudyId, DiabeticSmoker, CancellationToken.None);

            var untouched = db.PatientRiskFactors.Single(x => x.RiskFactorId == riskOfA.RiskFactorId);
            Assert.Equal(studyA.StudyId, untouched.StudyId);
            Assert.Equal((byte)0, untouched.Diabetes);

            Assert.Single(db.PatientRiskFactors.Where(x => x.StudyId == studyB.StudyId));
        }

        [Fact]
        public async Task UpsertByStudy_edits_the_most_recent_record_the_one_used_by_the_diagnosis()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db);
            var study = TestDb.AddStudy(db, patient.PatientId);
            var older = TestDb.AddRiskFactors(db, study.StudyId, createdAt: DateTime.Now.AddDays(-10));
            var newer = TestDb.AddRiskFactors(db, study.StudyId, createdAt: DateTime.Now);

            var controller = new RiskFactorsController(db, new DiagnosisQueue());

            await controller.UpsertByStudy(study.StudyId, DiabeticSmoker, CancellationToken.None);

            Assert.Equal((byte)1, db.PatientRiskFactors.Single(x => x.RiskFactorId == newer.RiskFactorId).Diabetes);
            Assert.Equal((byte)0, db.PatientRiskFactors.Single(x => x.RiskFactorId == older.RiskFactorId).Diabetes);
        }

        [Fact]
        public async Task UpsertByStudy_marks_the_study_as_queued_and_enqueues_a_new_diagnosis()
        {
            using var db = TestDb.Create();
            var patient = TestDb.AddPatient(db);
            var study = TestDb.AddStudy(db, patient.PatientId, StudyProcessingStatus.Completed);
            TestDb.AddRiskFactors(db, study.StudyId);

            var queue = new DiagnosisQueue();
            var controller = new RiskFactorsController(db, queue);

            await controller.UpsertByStudy(study.StudyId, DiabeticSmoker, CancellationToken.None);

            Assert.Equal(StudyProcessingStatus.Queued, db.MAPAStudies.Single().ProcessingStatus);
            Assert.True(queue.TryDequeue(out var job));
            Assert.Equal(study.StudyId, job.StudyId);
        }

        [Fact]
        public async Task UpsertByStudy_returns_404_for_a_study_that_does_not_exist()
        {
            using var db = TestDb.Create();

            var queue = new DiagnosisQueue();
            var controller = new RiskFactorsController(db, queue);

            var result = await controller.UpsertByStudy(9999, DiabeticSmoker, CancellationToken.None);

            var problem = Assert.IsType<ObjectResult>(result);
            Assert.Equal(404, problem.StatusCode);
            Assert.False(queue.TryDequeue(out _));
        }
    }
}

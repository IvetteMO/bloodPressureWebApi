using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalPatientCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Sex = table.Column<byte>(type: "tinyint", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "MAPAStudies",
                columns: table => new
                {
                    StudyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    ExternalStudyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudyDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AwakeTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    SleepTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    CustomSchedule = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExcelFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalReadings = table.Column<int>(type: "int", nullable: true),
                    CorrectReadingsPercent = table.Column<double>(type: "float", nullable: true),
                    StudyStatus = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "IMPORTED"),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAPAStudies", x => x.StudyId);
                    table.ForeignKey(
                        name: "FK_MAPAStudies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIDiagnosis",
                columns: table => new
                {
                    DiagnosisId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    FuzzyHypertensionRisk = table.Column<double>(type: "float", nullable: true),
                    FuzzyCardiovascularRisk = table.Column<double>(type: "float", nullable: true),
                    FuzzyNocturnalRisk = table.Column<double>(type: "float", nullable: true),
                    NeuralHypertensionRisk = table.Column<double>(type: "float", nullable: true),
                    NeuralCardiovascularRisk = table.Column<double>(type: "float", nullable: true),
                    NeuralNocturnalRisk = table.Column<double>(type: "float", nullable: true),
                    FinalRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeartAge = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIDiagnosis", x => x.DiagnosisId);
                    table.ForeignKey(
                        name: "FK_AIDiagnosis_MAPAStudies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "MAPAStudies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MAPAFeatures",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    DaySystolicMean = table.Column<double>(type: "float", nullable: true),
                    DayDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    NightSystolicMean = table.Column<double>(type: "float", nullable: true),
                    NightDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    MeanPulse = table.Column<double>(type: "float", nullable: true),
                    SystolicNightRatio = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: true),
                    DiastolicNightRatio = table.Column<double>(type: "float(10)", precision: 10, scale: 4, nullable: true),
                    DippingPercent = table.Column<double>(type: "float", nullable: true),
                    DayPressureLoad = table.Column<double>(type: "float", nullable: true),
                    NightPressureLoad = table.Column<double>(type: "float", nullable: true),
                    TotalPressureLoad = table.Column<double>(type: "float", nullable: true),
                    SystolicVariability = table.Column<double>(type: "float", nullable: true),
                    DiastolicVariability = table.Column<double>(type: "float", nullable: true),
                    PulseVariability = table.Column<double>(type: "float", nullable: true),
                    SystolicTrend = table.Column<double>(type: "float", nullable: true),
                    DiastolicTrend = table.Column<double>(type: "float", nullable: true),
                    PulseTrend = table.Column<double>(type: "float", nullable: true),
                    ValidReadingsPercent = table.Column<double>(type: "float", nullable: true),
                    ArtifactReadings = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAPAFeatures", x => x.FeatureId);
                    table.ForeignKey(
                        name: "FK_MAPAFeatures_MAPAStudies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "MAPAStudies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MAPAReadings",
                columns: table => new
                {
                    ReadingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    ReadingDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Systolic = table.Column<int>(type: "int", nullable: false),
                    Diastolic = table.Column<int>(type: "int", nullable: false),
                    Pulse = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAPAReadings", x => x.ReadingId);
                    table.ForeignKey(
                        name: "FK_MAPAReadings_MAPAStudies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "MAPAStudies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientRiskFactors",
                columns: table => new
                {
                    RiskFactorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: true),
                    Height = table.Column<double>(type: "float", nullable: true),
                    BMI = table.Column<double>(type: "float", nullable: true),
                    Smoking = table.Column<bool>(type: "bit", nullable: true),
                    Diabetes = table.Column<bool>(type: "bit", nullable: true),
                    HypertensionTreatment = table.Column<bool>(type: "bit", nullable: true),
                    ParentalHypertension = table.Column<byte>(type: "tinyint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientsPatientId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientRiskFactors", x => x.RiskFactorId);
                    table.ForeignKey(
                        name: "FK_PatientRiskFactors_MAPAStudies_StudyId",
                        column: x => x.StudyId,
                        principalTable: "MAPAStudies",
                        principalColumn: "StudyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PatientRiskFactors_Patients_PatientsPatientId",
                        column: x => x.PatientsPatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIDiagnosis_StudyId",
                table: "AIDiagnosis",
                column: "StudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MAPAFeatures_StudyId",
                table: "MAPAFeatures",
                column: "StudyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MAPAReadings_StudyId_ReadingDateTime",
                table: "MAPAReadings",
                columns: new[] { "StudyId", "ReadingDateTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MAPAStudies_PatientId",
                table: "MAPAStudies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientRiskFactors_PatientsPatientId",
                table: "PatientRiskFactors",
                column: "PatientsPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientRiskFactors_StudyId",
                table: "PatientRiskFactors",
                column: "StudyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIDiagnosis");

            migrationBuilder.DropTable(
                name: "MAPAFeatures");

            migrationBuilder.DropTable(
                name: "MAPAReadings");

            migrationBuilder.DropTable(
                name: "PatientRiskFactors");

            migrationBuilder.DropTable(
                name: "MAPAStudies");

            migrationBuilder.DropTable(
                name: "Patients");
        }
    }
}

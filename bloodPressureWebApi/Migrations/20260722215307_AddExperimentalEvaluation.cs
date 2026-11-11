using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExperimentalEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExperimentRuns",
                columns: table => new
                {
                    ExperimentRunId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExperimentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    Repetition = table.Column<int>(type: "int", nullable: false),
                    IsWarmup = table.Column<bool>(type: "bit", nullable: false),
                    ExecutionMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalMilliseconds = table.Column<double>(type: "float", nullable: false),
                    MemoryBeforeMB = table.Column<double>(type: "float", nullable: false),
                    MemoryAfterMB = table.Column<double>(type: "float", nullable: false),
                    MemoryDeltaMB = table.Column<double>(type: "float", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeaturePipelineVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatlabRuntimeVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExperimentRuns", x => x.ExperimentRunId);
                });

            migrationBuilder.CreateTable(
                name: "ScientificEquivalenceResults",
                columns: table => new
                {
                    EquivalenceResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    MatlabSystolicTrend = table.Column<double>(type: "float", nullable: false),
                    BackendSystolicTrend = table.Column<double>(type: "float", nullable: false),
                    SystolicTrendAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabDiastolicTrend = table.Column<double>(type: "float", nullable: false),
                    BackendDiastolicTrend = table.Column<double>(type: "float", nullable: false),
                    DiastolicTrendAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabPulseTrend = table.Column<double>(type: "float", nullable: false),
                    BackendPulseTrend = table.Column<double>(type: "float", nullable: false),
                    PulseTrendAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabBPRisk = table.Column<double>(type: "float", nullable: false),
                    BackendBPRisk = table.Column<double>(type: "float", nullable: false),
                    BPRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabHeartRateRisk = table.Column<double>(type: "float", nullable: false),
                    BackendHeartRateRisk = table.Column<double>(type: "float", nullable: false),
                    HeartRateAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabLoadRisk = table.Column<double>(type: "float", nullable: false),
                    BackendLoadRisk = table.Column<double>(type: "float", nullable: false),
                    LoadAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    BackendNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    NocturnalAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabNocturnalProfile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackendNocturnalProfile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NocturnalProfileMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabHypertensionRisk = table.Column<double>(type: "float", nullable: false),
                    BackendHypertensionRisk = table.Column<double>(type: "float", nullable: false),
                    HypertensionRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabCardiovascularRisk = table.Column<double>(type: "float", nullable: false),
                    BackendCardiovascularRisk = table.Column<double>(type: "float", nullable: false),
                    CardiovascularRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabHeartAge = table.Column<double>(type: "float", nullable: false),
                    BackendHeartAge = table.Column<double>(type: "float", nullable: false),
                    HeartAgeAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabFinalRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackendFinalRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalRiskLevelMatch = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScientificEquivalenceResults", x => x.EquivalenceResultId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExperimentRuns");

            migrationBuilder.DropTable(
                name: "ScientificEquivalenceResults");
        }
    }
}

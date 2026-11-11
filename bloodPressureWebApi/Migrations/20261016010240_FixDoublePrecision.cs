using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class FixDoublePrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScientificEquivalenceResults");

            migrationBuilder.AlterColumn<double>(
                name: "SystolicNightRatio",
                table: "MAPAFeatures",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(10)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DiastolicNightRatio",
                table: "MAPAFeatures",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(10)",
                oldPrecision: 10,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "HeartAge",
                table: "AIDiagnosis",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(10)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "E1BValidationResults",
                columns: table => new
                {
                    ValidationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MatlabSysANN = table.Column<double>(type: "float", nullable: false),
                    BackendSysANN = table.Column<double>(type: "float", nullable: false),
                    SysANNAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    SysANNRelativeError = table.Column<double>(type: "float", nullable: false),
                    SysANNMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabDiaANN = table.Column<double>(type: "float", nullable: false),
                    BackendDiaANN = table.Column<double>(type: "float", nullable: false),
                    DiaANNAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    DiaANNRelativeError = table.Column<double>(type: "float", nullable: false),
                    DiaANNMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabPulseANN = table.Column<double>(type: "float", nullable: false),
                    BackendPulseANN = table.Column<double>(type: "float", nullable: false),
                    PulseANNAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    PulseANNRelativeError = table.Column<double>(type: "float", nullable: false),
                    PulseANNMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabBPLevel = table.Column<double>(type: "float", nullable: false),
                    BackendBPLevel = table.Column<double>(type: "float", nullable: false),
                    BPLevelAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    BPLevelRelativeError = table.Column<double>(type: "float", nullable: false),
                    BPLevelMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabHeartRate = table.Column<double>(type: "float", nullable: false),
                    BackendHeartRate = table.Column<double>(type: "float", nullable: false),
                    HeartRateAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    HeartRateRelativeError = table.Column<double>(type: "float", nullable: false),
                    HeartRateMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabDaySys = table.Column<double>(type: "float", nullable: false),
                    BackendDaySys = table.Column<double>(type: "float", nullable: false),
                    DaySysAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    DaySysRelativeError = table.Column<double>(type: "float", nullable: false),
                    DaySysMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabDayDia = table.Column<double>(type: "float", nullable: false),
                    BackendDayDia = table.Column<double>(type: "float", nullable: false),
                    DayDiaAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    DayDiaRelativeError = table.Column<double>(type: "float", nullable: false),
                    DayDiaMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabNightSys = table.Column<double>(type: "float", nullable: false),
                    BackendNightSys = table.Column<double>(type: "float", nullable: false),
                    NightSysAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    NightSysRelativeError = table.Column<double>(type: "float", nullable: false),
                    NightSysMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabNightDia = table.Column<double>(type: "float", nullable: false),
                    BackendNightDia = table.Column<double>(type: "float", nullable: false),
                    NightDiaAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    NightDiaRelativeError = table.Column<double>(type: "float", nullable: false),
                    NightDiaMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabCsis = table.Column<double>(type: "float", nullable: false),
                    BackendCsis = table.Column<double>(type: "float", nullable: false),
                    CsisAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    CsisRelativeError = table.Column<double>(type: "float", nullable: false),
                    CsisMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabCdia = table.Column<double>(type: "float", nullable: false),
                    BackendCdia = table.Column<double>(type: "float", nullable: false),
                    CdiaAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    CdiaRelativeError = table.Column<double>(type: "float", nullable: false),
                    CdiaMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    BackendNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    NocturnalAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    NocturnalRelativeError = table.Column<double>(type: "float", nullable: false),
                    NocturnalMatch = table.Column<bool>(type: "bit", nullable: false),
                    MatlabProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackendProfile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileMatch = table.Column<bool>(type: "bit", nullable: false),
                    TotalComparisons = table.Column<int>(type: "int", nullable: false),
                    PassedComparisons = table.Column<int>(type: "int", nullable: false),
                    FailedComparisons = table.Column<int>(type: "int", nullable: false),
                    AgreementPercentage = table.Column<double>(type: "float", nullable: false),
                    ValidationPassed = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_E1BValidationResults", x => x.ValidationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "E1BValidationResults");

            migrationBuilder.AlterColumn<double>(
                name: "SystolicNightRatio",
                table: "MAPAFeatures",
                type: "float(10)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "DiastolicNightRatio",
                table: "MAPAFeatures",
                type: "float(10)",
                precision: 10,
                scale: 4,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "HeartAge",
                table: "AIDiagnosis",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ScientificEquivalenceResults",
                columns: table => new
                {
                    EquivalenceResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BPRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    BackendBPRisk = table.Column<double>(type: "float", nullable: false),
                    BackendCardiovascularRisk = table.Column<double>(type: "float", nullable: false),
                    BackendDiastolicTrend = table.Column<double>(type: "float", nullable: false),
                    BackendFinalRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackendHeartAge = table.Column<double>(type: "float", nullable: false),
                    BackendHeartRateRisk = table.Column<double>(type: "float", nullable: false),
                    BackendHypertensionRisk = table.Column<double>(type: "float", nullable: false),
                    BackendLoadRisk = table.Column<double>(type: "float", nullable: false),
                    BackendNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    BackendNocturnalProfile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackendPulseTrend = table.Column<double>(type: "float", nullable: false),
                    BackendSystolicTrend = table.Column<double>(type: "float", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardiovascularRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiastolicTrendAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    FinalRiskLevelMatch = table.Column<bool>(type: "bit", nullable: false),
                    HeartAgeAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    HeartRateAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    HypertensionRiskAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    LoadAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    MatlabBPRisk = table.Column<double>(type: "float", nullable: false),
                    MatlabCardiovascularRisk = table.Column<double>(type: "float", nullable: false),
                    MatlabDiastolicTrend = table.Column<double>(type: "float", nullable: false),
                    MatlabFinalRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatlabHeartAge = table.Column<double>(type: "float", nullable: false),
                    MatlabHeartRateRisk = table.Column<double>(type: "float", nullable: false),
                    MatlabHypertensionRisk = table.Column<double>(type: "float", nullable: false),
                    MatlabLoadRisk = table.Column<double>(type: "float", nullable: false),
                    MatlabNocturnalOutput = table.Column<double>(type: "float", nullable: false),
                    MatlabNocturnalProfile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatlabPulseTrend = table.Column<double>(type: "float", nullable: false),
                    MatlabSystolicTrend = table.Column<double>(type: "float", nullable: false),
                    NocturnalAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    NocturnalProfileMatch = table.Column<bool>(type: "bit", nullable: false),
                    PulseTrendAbsoluteError = table.Column<double>(type: "float", nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    SystolicTrendAbsoluteError = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScientificEquivalenceResults", x => x.EquivalenceResultId);
                });
        }
    }
}

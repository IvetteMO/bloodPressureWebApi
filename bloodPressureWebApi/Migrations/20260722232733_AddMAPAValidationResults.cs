using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMAPAValidationResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MAPAValidationResults",
                columns: table => new
                {
                    ValidationResultId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExperimentCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StudyId = table.Column<int>(type: "int", nullable: false),
                    ExternalPatientCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SourceFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalValidReadings = table.Column<int>(type: "int", nullable: true),
                    BackendValidReadings = table.Column<int>(type: "int", nullable: true),
                    ValidReadingsAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    OriginalDaySystolicMean = table.Column<double>(type: "float", nullable: true),
                    BackendDaySystolicMean = table.Column<double>(type: "float", nullable: true),
                    DaySystolicAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    DaySystolicRelativeError = table.Column<double>(type: "float", nullable: true),
                    OriginalDayDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    BackendDayDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    DayDiastolicAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    DayDiastolicRelativeError = table.Column<double>(type: "float", nullable: true),
                    OriginalNightSystolicMean = table.Column<double>(type: "float", nullable: true),
                    BackendNightSystolicMean = table.Column<double>(type: "float", nullable: true),
                    NightSystolicAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    NightSystolicRelativeError = table.Column<double>(type: "float", nullable: true),
                    OriginalNightDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    BackendNightDiastolicMean = table.Column<double>(type: "float", nullable: true),
                    NightDiastolicAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    NightDiastolicRelativeError = table.Column<double>(type: "float", nullable: true),
                    OriginalMeanPulse = table.Column<double>(type: "float", nullable: true),
                    BackendMeanPulse = table.Column<double>(type: "float", nullable: true),
                    MeanPulseAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    MeanPulseRelativeError = table.Column<double>(type: "float", nullable: true),
                    OriginalDayPressureLoad = table.Column<double>(type: "float", nullable: true),
                    BackendDayPressureLoad = table.Column<double>(type: "float", nullable: true),
                    DayPressureLoadAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    OriginalNightPressureLoad = table.Column<double>(type: "float", nullable: true),
                    BackendNightPressureLoad = table.Column<double>(type: "float", nullable: true),
                    NightPressureLoadAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    OriginalSystolicDippingPercent = table.Column<double>(type: "float", nullable: true),
                    BackendSystolicDippingPercent = table.Column<double>(type: "float", nullable: true),
                    SystolicDippingAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    OriginalDiastolicDippingPercent = table.Column<double>(type: "float", nullable: true),
                    BackendDiastolicDippingPercent = table.Column<double>(type: "float", nullable: true),
                    DiastolicDippingAbsoluteError = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAPAValidationResults", x => x.ValidationResultId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MAPAValidationResults");
        }
    }
}

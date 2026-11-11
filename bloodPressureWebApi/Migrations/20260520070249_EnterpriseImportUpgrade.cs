using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseImportUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "MAPAStudies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PreviousStudyId",
                table: "MAPAStudies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingError",
                table: "MAPAStudies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingStatus",
                table: "MAPAStudies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "MAPAStudies",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "MAPAStudies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "AIDiagnosis",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeaturePipelineVersion",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MatlabRuntimeVersion",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "PreviousStudyId",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "ProcessingError",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "ProcessingStatus",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "MAPAStudies");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "AIDiagnosis");

            migrationBuilder.DropColumn(
                name: "FeaturePipelineVersion",
                table: "AIDiagnosis");

            migrationBuilder.DropColumn(
                name: "MatlabRuntimeVersion",
                table: "AIDiagnosis");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "AIDiagnosis");
        }
    }
}

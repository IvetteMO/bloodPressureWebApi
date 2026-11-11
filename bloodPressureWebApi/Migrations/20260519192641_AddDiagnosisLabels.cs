using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnosisLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeartRateDiagnosis",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoadDiagnosis",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeartRateDiagnosis",
                table: "AIDiagnosis");

            migrationBuilder.DropColumn(
                name: "LoadDiagnosis",
                table: "AIDiagnosis");
        }
    }
}

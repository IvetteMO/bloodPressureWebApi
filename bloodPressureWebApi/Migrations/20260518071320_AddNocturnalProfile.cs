using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNocturnalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeuralHypertensionRisk",
                table: "AIDiagnosis");

            migrationBuilder.DropColumn(
                name: "NeuralNocturnalRisk",
                table: "AIDiagnosis");

            migrationBuilder.AddColumn<string>(
                name: "NocturnalProfile",
                table: "AIDiagnosis",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NocturnalProfile",
                table: "AIDiagnosis");

            migrationBuilder.AddColumn<double>(
                name: "NeuralHypertensionRisk",
                table: "AIDiagnosis",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NeuralNocturnalRisk",
                table: "AIDiagnosis",
                type: "float",
                nullable: true);
        }
    }
}

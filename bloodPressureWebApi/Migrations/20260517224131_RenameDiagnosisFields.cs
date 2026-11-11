using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameDiagnosisFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NeuralCardiovascularRisk",
                table: "AIDiagnosis",
                newName: "FuzzyNocturnalC");

            migrationBuilder.RenameColumn(
                name: "FuzzyNocturnalRisk",
                table: "AIDiagnosis",
                newName: "FuzzyLoadC");

            migrationBuilder.RenameColumn(
                name: "FuzzyHypertensionRisk",
                table: "AIDiagnosis",
                newName: "FuzzyHeartRC");

            migrationBuilder.RenameColumn(
                name: "FuzzyCardiovascularRisk",
                table: "AIDiagnosis",
                newName: "FuzzyBPRisk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FuzzyNocturnalC",
                table: "AIDiagnosis",
                newName: "NeuralCardiovascularRisk");

            migrationBuilder.RenameColumn(
                name: "FuzzyLoadC",
                table: "AIDiagnosis",
                newName: "FuzzyNocturnalRisk");

            migrationBuilder.RenameColumn(
                name: "FuzzyHeartRC",
                table: "AIDiagnosis",
                newName: "FuzzyHypertensionRisk");

            migrationBuilder.RenameColumn(
                name: "FuzzyBPRisk",
                table: "AIDiagnosis",
                newName: "FuzzyCardiovascularRisk");
        }
    }
}

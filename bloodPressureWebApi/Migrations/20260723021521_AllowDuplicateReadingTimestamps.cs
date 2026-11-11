using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bloodPressureWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateReadingTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MAPAReadings_StudyId_ReadingDateTime",
                table: "MAPAReadings");

            migrationBuilder.CreateIndex(
                name: "IX_MAPAReadings_StudyId_ReadingDateTime",
                table: "MAPAReadings",
                columns: new[] { "StudyId", "ReadingDateTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MAPAReadings_StudyId_ReadingDateTime",
                table: "MAPAReadings");

            migrationBuilder.CreateIndex(
                name: "IX_MAPAReadings_StudyId_ReadingDateTime",
                table: "MAPAReadings",
                columns: new[] { "StudyId", "ReadingDateTime" },
                unique: true);
        }
    }
}

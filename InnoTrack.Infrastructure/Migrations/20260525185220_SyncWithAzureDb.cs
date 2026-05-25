using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnoTrack.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncWithAzureDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "SimilarProjects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AcademicYearId1",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNames",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimilarProjects_ProjectId",
                table: "SimilarProjects",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_AcademicYearId1",
                table: "Projects",
                column: "AcademicYearId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_AcademicYears_AcademicYearId1",
                table: "Projects",
                column: "AcademicYearId1",
                principalTable: "AcademicYears",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SimilarProjects_Projects_ProjectId",
                table: "SimilarProjects",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_AcademicYears_AcademicYearId1",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_SimilarProjects_Projects_ProjectId",
                table: "SimilarProjects");

            migrationBuilder.DropIndex(
                name: "IX_SimilarProjects_ProjectId",
                table: "SimilarProjects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_AcademicYearId1",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "SimilarProjects");

            migrationBuilder.DropColumn(
                name: "AcademicYearId1",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StudentNames",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Projects");
        }
    }
}

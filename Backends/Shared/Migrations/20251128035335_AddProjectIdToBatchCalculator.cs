using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectIdToBatchCalculator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "project_id",
                table: "batch_calculators",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_batch_calculators_project_id",
                table: "batch_calculators",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_batch_calculators_projects_project_id",
                table: "batch_calculators",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_batch_calculators_projects_project_id",
                table: "batch_calculators");

            migrationBuilder.DropIndex(
                name: "IX_batch_calculators_project_id",
                table: "batch_calculators");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "batch_calculators");
        }
    }
}

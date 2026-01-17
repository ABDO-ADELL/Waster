using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waster.Migrations
{
    /// <inheritdoc />
    public partial class DAshboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dashboardStatus_UserId",
                table: "dashboardStatus");

            migrationBuilder.RenameColumn(
                name: "MealsServed",
                table: "dashboardStatus",
                newName: "MealsServedInKG");

            migrationBuilder.AddColumn<int>(
                name: "Monthlygoals",
                table: "dashboardStatus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PendingClaims",
                table: "dashboardStatus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalClaims",
                table: "dashboardStatus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardStats_UserId_Unique",
                table: "dashboardStatus",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DashboardStats_UserId_Unique",
                table: "dashboardStatus");

            migrationBuilder.DropColumn(
                name: "Monthlygoals",
                table: "dashboardStatus");

            migrationBuilder.DropColumn(
                name: "PendingClaims",
                table: "dashboardStatus");

            migrationBuilder.DropColumn(
                name: "TotalClaims",
                table: "dashboardStatus");

            migrationBuilder.RenameColumn(
                name: "MealsServedInKG",
                table: "dashboardStatus",
                newName: "MealsServed");

            migrationBuilder.CreateIndex(
                name: "IX_dashboardStatus_UserId",
                table: "dashboardStatus",
                column: "UserId");
        }
    }
}

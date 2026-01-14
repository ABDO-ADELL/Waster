using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waster.Migrations
{
    /// <inheritdoc />
    public partial class Dashboardstats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveUsers",
                table: "dashboardStatus");

            migrationBuilder.AlterColumn<double>(
                name: "MealsServed",
                table: "dashboardStatus",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "dashboardStatus",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_dashboardStatus_UserId",
                table: "dashboardStatus",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_dashboardStatus_AspNetUsers_UserId",
                table: "dashboardStatus",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_dashboardStatus_AspNetUsers_UserId",
                table: "dashboardStatus");

            migrationBuilder.DropIndex(
                name: "IX_dashboardStatus_UserId",
                table: "dashboardStatus");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "dashboardStatus");

            migrationBuilder.AlterColumn<int>(
                name: "MealsServed",
                table: "dashboardStatus",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AddColumn<int>(
                name: "ActiveUsers",
                table: "dashboardStatus",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

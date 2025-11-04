using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waster.Migrations
{
    /// <inheritdoc />
    public partial class Index : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_ClaimPosts_PostId",
                table: "ClaimPosts");

            migrationBuilder.DropIndex(
                name: "IX_ClaimPosts_RecipientId",
                table: "ClaimPosts");

            migrationBuilder.DropColumn(
                name: "ImageData",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "ImageType",
                table: "Posts",
                newName: "ImageUrl");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Posts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Posts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClaimPosts",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ExpiresOn",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresOn" });

            migrationBuilder.CreateIndex(
                name: "IX_Post_Category",
                table: "Posts",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Post_ExpiresOn",
                table: "Posts",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_Post_IsDeleted",
                table: "Posts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Post_Status",
                table: "Posts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Post_UserId_IsDeleted_Status",
                table: "Posts",
                columns: new[] { "UserId", "IsDeleted", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Post_UserId_Status_IsDeleted",
                table: "Posts",
                columns: new[] { "UserId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPost_PostId_Status",
                table: "ClaimPosts",
                columns: new[] { "PostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPost_RecipientId_Status",
                table: "ClaimPosts",
                columns: new[] { "RecipientId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_ExpiresOn",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Post_Category",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Post_ExpiresOn",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Post_IsDeleted",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Post_Status",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Post_UserId_IsDeleted_Status",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Post_UserId_Status_IsDeleted",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_ClaimPost_PostId_Status",
                table: "ClaimPosts");

            migrationBuilder.DropIndex(
                name: "IX_ClaimPost_RecipientId_Status",
                table: "ClaimPosts");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Posts",
                newName: "ImageType");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<byte[]>(
                name: "ImageData",
                table: "Posts",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClaimPosts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPosts_PostId",
                table: "ClaimPosts",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimPosts_RecipientId",
                table: "ClaimPosts",
                column: "RecipientId");
        }
    }
}

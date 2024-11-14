using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeclutterHub.Migrations
{
    /// <inheritdoc />
    public partial class saveditemchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedItem_AspNetUsers_UserId1",
                table: "SavedItem");

            migrationBuilder.DropIndex(
                name: "IX_SavedItem_UserId1",
                table: "SavedItem");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "SavedItem");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "SavedItem",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SavedItem_UserId",
                table: "SavedItem",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedItem_AspNetUsers_UserId",
                table: "SavedItem",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SavedItem_AspNetUsers_UserId",
                table: "SavedItem");

            migrationBuilder.DropIndex(
                name: "IX_SavedItem_UserId",
                table: "SavedItem");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "SavedItem",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "SavedItem",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedItem_UserId1",
                table: "SavedItem",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_SavedItem_AspNetUsers_UserId1",
                table: "SavedItem",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}

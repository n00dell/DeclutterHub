using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeclutterHub.Migrations
{
    /// <inheritdoc />
    public partial class Countrycode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Item",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Item");
        }
    }
}

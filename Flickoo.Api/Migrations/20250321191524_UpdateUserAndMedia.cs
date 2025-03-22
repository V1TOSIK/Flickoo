using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flickoo.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAndMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Registered",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Registered",
                table: "Users");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flickoo.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMediaEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypeOfFile",
                table: "MediaFiles",
                newName: "TypeOfMedia");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TypeOfMedia",
                table: "MediaFiles",
                newName: "TypeOfFile");
        }
    }
}

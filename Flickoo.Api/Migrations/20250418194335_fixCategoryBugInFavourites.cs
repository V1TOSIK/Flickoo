using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flickoo.Api.Migrations
{
    /// <inheritdoc />
    public partial class fixCategoryBugInFavourites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favourites_UserId",
                table: "Favourites");

            migrationBuilder.CreateIndex(
                name: "IX_Favourites_UserId_ProductId",
                table: "Favourites",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Favourites_UserId_ProductId",
                table: "Favourites");

            migrationBuilder.CreateIndex(
                name: "IX_Favourites_UserId",
                table: "Favourites",
                column: "UserId");
        }
    }
}

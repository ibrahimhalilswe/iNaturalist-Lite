using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iNaturalist_Lite.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_plant_comments_plant_id",
                table: "plant_comments",
                column: "plant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_plant_comments_plants_plant_id",
                table: "plant_comments",
                column: "plant_id",
                principalTable: "plants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_plant_likes_plants_plant_id",
                table: "plant_likes",
                column: "plant_id",
                principalTable: "plants",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_plant_comments_plants_plant_id",
                table: "plant_comments");

            migrationBuilder.DropForeignKey(
                name: "FK_plant_likes_plants_plant_id",
                table: "plant_likes");

            migrationBuilder.DropIndex(
                name: "IX_plant_comments_plant_id",
                table: "plant_comments");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MobieleTijdkaart.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Projecten",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Projecten_UserId",
                table: "Projecten",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projecten_UserId",
                table: "Projecten");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Projecten");
        }
    }
}

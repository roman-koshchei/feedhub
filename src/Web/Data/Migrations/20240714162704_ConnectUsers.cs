using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web.Migrations
{
    /// <inheritdoc />
    public partial class ConnectUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Apps",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Apps_UserId",
                table: "Apps",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Apps_AspNetUsers_UserId",
                table: "Apps",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Apps_AspNetUsers_UserId",
                table: "Apps");

            migrationBuilder.DropIndex(
                name: "IX_Apps_UserId",
                table: "Apps");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Apps");
        }
    }
}

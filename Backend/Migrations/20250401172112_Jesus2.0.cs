using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Jesus20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_free_accounts_UserID",
                table: "free_accounts",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_free_accounts_users_UserID",
                table: "free_accounts",
                column: "UserID",
                principalTable: "users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_free_accounts_users_UserID",
                table: "free_accounts");

            migrationBuilder.DropIndex(
                name: "IX_free_accounts_UserID",
                table: "free_accounts");
        }
    }
}

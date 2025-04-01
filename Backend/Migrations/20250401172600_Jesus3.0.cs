using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Jesus30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_logs_DestAccID",
                table: "logs",
                column: "DestAccID");

            migrationBuilder.CreateIndex(
                name: "IX_logs_SrcAccID",
                table: "logs",
                column: "SrcAccID");

            migrationBuilder.CreateIndex(
                name: "IX_logs_UserID",
                table: "logs",
                column: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_logs_free_accounts_DestAccID",
                table: "logs",
                column: "DestAccID",
                principalTable: "free_accounts",
                principalColumn: "AccID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_logs_free_accounts_SrcAccID",
                table: "logs",
                column: "SrcAccID",
                principalTable: "free_accounts",
                principalColumn: "AccID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_logs_users_UserID",
                table: "logs",
                column: "UserID",
                principalTable: "users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_logs_free_accounts_DestAccID",
                table: "logs");

            migrationBuilder.DropForeignKey(
                name: "FK_logs_free_accounts_SrcAccID",
                table: "logs");

            migrationBuilder.DropForeignKey(
                name: "FK_logs_users_UserID",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_DestAccID",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_SrcAccID",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_UserID",
                table: "logs");
        }
    }
}

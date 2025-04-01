using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class TPT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Statistics",
                table: "Statistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Logs",
                table: "Logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FreeAccounts",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "DailyWithdrawal",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "MaturityDate",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "MonthlyHistory",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "MonthlyHistoryLastUpdated",
                table: "FreeAccounts");

            migrationBuilder.DropColumn(
                name: "Student",
                table: "FreeAccounts");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "Statistics",
                newName: "statistics");

            migrationBuilder.RenameTable(
                name: "Logs",
                newName: "logs");

            migrationBuilder.RenameTable(
                name: "FreeAccounts",
                newName: "free_accounts");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_statistics",
                table: "statistics",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_logs",
                table: "logs",
                column: "LogID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_free_accounts",
                table: "free_accounts",
                column: "AccID");

            migrationBuilder.CreateTable(
                name: "credit_accounts",
                columns: table => new
                {
                    AccID = table.Column<int>(type: "int", nullable: false),
                    MaturityDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_accounts", x => x.AccID);
                    table.ForeignKey(
                        name: "FK_credit_accounts_free_accounts_AccID",
                        column: x => x.AccID,
                        principalTable: "free_accounts",
                        principalColumn: "AccID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "saving_accounts",
                columns: table => new
                {
                    AccID = table.Column<int>(type: "int", nullable: false),
                    Student = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DailyWithdrawal = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MonthlyHistory = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MonthlyHistoryLastUpdated = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saving_accounts", x => x.AccID);
                    table.ForeignKey(
                        name: "FK_saving_accounts_free_accounts_AccID",
                        column: x => x.AccID,
                        principalTable: "free_accounts",
                        principalColumn: "AccID",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "credit_accounts");

            migrationBuilder.DropTable(
                name: "saving_accounts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_statistics",
                table: "statistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_logs",
                table: "logs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_free_accounts",
                table: "free_accounts");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "statistics",
                newName: "Statistics");

            migrationBuilder.RenameTable(
                name: "logs",
                newName: "Logs");

            migrationBuilder.RenameTable(
                name: "free_accounts",
                newName: "FreeAccounts");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyWithdrawal",
                table: "FreeAccounts",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "FreeAccounts",
                type: "varchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MaturityDate",
                table: "FreeAccounts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MonthlyHistory",
                table: "FreeAccounts",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "MonthlyHistoryLastUpdated",
                table: "FreeAccounts",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Student",
                table: "FreeAccounts",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Statistics",
                table: "Statistics",
                column: "ID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Logs",
                table: "Logs",
                column: "LogID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FreeAccounts",
                table: "FreeAccounts",
                column: "AccID");
        }
    }
}

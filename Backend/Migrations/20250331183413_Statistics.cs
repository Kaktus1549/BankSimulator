using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class Statistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Statistics",
                columns: table => new
                {
                    TotalUserCount = table.Column<int>(type: "int", nullable: false),
                    TotalAccountCount = table.Column<int>(type: "int", nullable: false),
                    TotalTransactionCount = table.Column<int>(type: "int", nullable: false),
                    FreeAccountCount = table.Column<int>(type: "int", nullable: false),
                    SavingAccountCount = table.Column<int>(type: "int", nullable: false),
                    CreditAccountCount = table.Column<int>(type: "int", nullable: false),
                    TotalDebt30Days = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Statistics");
        }
    }
}

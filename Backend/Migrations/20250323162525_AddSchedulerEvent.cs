using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE EVENT IF NOT EXISTS daily_clear_withdrawal
                ON SCHEDULE EVERY 1 DAY
                DO
                    UPDATE SavingAccounts
                    SET DailyWithdrawal = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP EVENT IF EXISTS daily_clear_withdrawal;");
        }
    }
}

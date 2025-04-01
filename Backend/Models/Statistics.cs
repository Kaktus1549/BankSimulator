using System.ComponentModel.DataAnnotations;

public class DBStatistics{
    [Key]
    public int ID { get; set; }
    public int TotalUserCount { get; set; }
    public int TotalAdminCount { get; set; }
    public int TotalUsersCount { get; set; }
    public int TotalBankerCount { get; set; }
    public int TotalAccountCount { get; set; }
    public int TotalTransactionCount { get; set; }
    public int FreeAccountCount { get; set; }
    public int SavingAccountCount { get; set; }
    public int CreditAccountCount { get; set; }
    public decimal[]? TotalDebt30Days { get; set; }
    public DateTime LastUpdate {get; set;}
}
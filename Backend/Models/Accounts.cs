using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bogus;

public enum AccountType{
    Free,
    Saving,
    Credit
}

public class DBFreeAccount{
    [Key]
    public int AccID { get; set; }
    [ForeignKey("UserID")]
    public DBUser? User { get; set; }
    public int UserID { get; set; }

    public decimal Balance { get; set; }
    public decimal[]? BalanceHistory { get; set; }
    public DateTime BalanceLastUpdated { get; set; }
}
public class DBSavingAccount : DBFreeAccount{
    public bool Student { get; set; }
    public decimal DailyWithdrawal { get; set; }
    public decimal[]? MonthlyHistory { get; set; }
    public DateTime MonthlyHistoryLastUpdated { get; set; }
}
public class DBCreditAccount : DBFreeAccount{
    public DateTime MaturityDate { get; set; }
}

public class AccountIDGenerator{
    public static Faker faker = new Faker();
    public static int GenerateAccountID(){
        return int.Parse(faker.Finance.Account());
    }

}

public class FreeAccount{
    protected int _AccountID { get; set; }
    private int _UserID { get; set; }
    private decimal _Balance { get; set; }
    private decimal[] _BalanceHistory { get; set; }

    public FreeAccount(int AccountID, int UserID, decimal Balance, decimal[]? BalanceHistory = null){
        _AccountID = AccountID;
        _UserID = UserID;
        _Balance = Balance;
        _BalanceHistory = BalanceHistory ?? new decimal[0];
    }

    public string AccountNumber{
        get{
            return _AccountID.ToString() + "/001";
        }
    }
    public double Balance{
        get{
            return (double)_Balance;
        }
    }
}

public class SavingAccount : FreeAccount{
    private bool _Student { get; set; }
    private decimal _DailyWithdrawal { get; set; }
    private decimal[] _MonthlyHistory { get; set; }

    public SavingAccount(int AccountID, int UserID, decimal Balance, bool Student, decimal DailyWithdrawal, decimal[]? MonthlyHistory = null) : base(AccountID, UserID, Balance){
        _Student = Student;
        _DailyWithdrawal = DailyWithdrawal;
        _MonthlyHistory = MonthlyHistory ?? new decimal[0];
    }

    new public string AccountNumber{
        get{
            return _AccountID.ToString() + "/002";
        }
    }
}

public class CreditAccount : FreeAccount{
    private DateTime _MaturityDate { get; set; }

    public CreditAccount(int AccountID, int UserID, decimal Balance, DateTime MaturityDate) : base(AccountID, UserID, Balance){
        _MaturityDate = MaturityDate;
    }

    new public string AccountNumber{
        get{
            return _AccountID.ToString() + "/003";
        }
    }
}
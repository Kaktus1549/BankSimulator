using System.ComponentModel.DataAnnotations;
using Bogus;

public enum AccountType{
    Free,
    Saving,
    Credit
}

public class DBFreeAccount{
    public int AccID { get; set; }
    public int UserID { get; set; }
    public decimal Balance { get; set; }
}
public class DBSavingAccount : DBFreeAccount{
    public bool Student { get; set; }
}
public class DBCreditAccount : DBFreeAccount{
    public DateTime MaturityDate { get; set; }
}

public class AccountIDGenerator{
    public static Faker faker = new Faker();
    public static int GenerateAccountID(AccountType type){
        string suffix = "";
        switch(type){
            case AccountType.Free:
                suffix = "/001";
                break;
            case AccountType.Saving:
                suffix = "/002";
                break;
            case AccountType.Credit:
                suffix = "/003";
                break;
        }
        return int.Parse(faker.Finance.Account() + suffix);
    }

}

public class FreeAccount{
    private int _AccountID { get; set; }
    private int _UserID { get; set; }
    private decimal _Balance { get; set; }

    public FreeAccount(int AccountID, int UserID, decimal Balance){
        _AccountID = AccountID;
        _UserID = UserID;
        _Balance = Balance;
    }
}

public class SavingAccount : FreeAccount{
    private bool _Student { get; set; }

    public SavingAccount(int AccountID, int UserID, decimal Balance, bool Student) : base(AccountID, UserID, Balance){
        _Student = Student;
    }
}

public class CreditAccount : FreeAccount{
    private DateTime _MaturityDate { get; set; }

    public CreditAccount(int AccountID, int UserID, decimal Balance, DateTime MaturityDate) : base(AccountID, UserID, Balance){
        _MaturityDate = MaturityDate;
    }
}
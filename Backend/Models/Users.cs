public enum Role{
    User,
    Banker,
    Admin
}

public class DBUser{
    public int UserID { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public Role Role { get; set; }
    public bool Bankrupt { get; set; }
}

public class User{
    private string _FirstName;
    private string _LastName;
    private string _Email;
    private int _UserID;
    private bool _Bankrupt;
    private FreeAccount? _Account;
    private SavingAccount? _SavingAccount;
    private CreditAccount? _CreditAccount;

    public User(string FirstName, string LastName, string Email, int UserID, FreeAccount? Account, SavingAccount? SavingAccount, CreditAccount? CreditAccount, bool Bankrupt = false){
        _Bankrupt = Bankrupt;
        _FirstName = FirstName;
        _LastName = LastName;
        _Email = Email;
        _UserID = UserID;
        _Account = Account;
        _SavingAccount = SavingAccount;
        _CreditAccount = CreditAccount;
    }
}

public class Banker : User{
    // Banker doesnt need accounts
    public Banker(string FirstName, string LastName, string Email, int UserID) : base(FirstName, LastName, Email, UserID, null, null, null, false){
    }
}

public class Admin : Banker{
    // Admin doesnt need accounts too
    public Admin(string FirstName, string LastName, string Email, int UserID) : base(FirstName, LastName, Email, UserID){
    }
}
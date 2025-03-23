using Microsoft.EntityFrameworkCore;

public class BankaDB : DbContext{
    public DbSet<DBUser> Users { get; set; }
    public DbSet<DBLog> Logs { get; set; }
    public DbSet<DBFreeAccount> FreeAccounts { get; set; }
    public DbSet<DBSavingAccount> SavingAccounts { get; set; }
    public DbSet<DBCreditAccount> CreditAccounts { get; set; }
    private string _serverAddress;
    private string _databaseName;
    private string _username;
    private string _password;
    private int _port;
    private decimal _maxDebt;
    private decimal _maxStudentWithdrawal;
    private decimal _maxStudentDailyWithdrawal;
    private float _interestRate;
    private readonly Serilog.ILogger _logger;


    public BankaDB(string serverAddress, string databaseName, string username, string password, Serilog.ILogger logger, decimal maxDebt = 10000, decimal maxStudentWithdrawal = 2000, decimal maxStudentDailyWithdrawal= 4000, float interestRate = 0.05f, int port = 3306){
        if (string.IsNullOrEmpty(serverAddress))
            throw new Exception("Server address is required");
        if (string.IsNullOrEmpty(databaseName))
            throw new Exception("Database name is required");
        if (string.IsNullOrEmpty(username))
            throw new Exception("Username is required");
        if (string.IsNullOrEmpty(password))
            throw new Exception("Password is required");

        _serverAddress = serverAddress;
        _databaseName = databaseName;
        _username = username;
        _password = password;
        _port = port;
        _maxDebt = maxDebt;
        _interestRate = interestRate;
        _logger = logger;
        _maxStudentDailyWithdrawal = maxStudentDailyWithdrawal;
        _maxStudentWithdrawal = maxStudentWithdrawal;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){
        string connectionString = $"Server={_serverAddress};Port={_port};Database={_databaseName};Uid={_username};Pwd={_password};";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), 
                                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
        _logger.Information("Database connection established!");
    }

    public void AddUser(RegisterDTO user){
        if (Users.Any(u => u.Email == user.Email)){
            throw new Exception("User with this email already exists");
        }
        DBUser dbUser = new DBUser{
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PasswordHash = PasswordService.HashPassword(user.Password),
            Role = user.Role,
            Bankrupt = false
        };

        Users.Add(dbUser);
        SaveChanges();
        _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has been registered");
        if (user.Role == Role.User){
            // Create free account, saving account and credit account
            DBFreeAccount freeAccount = new DBFreeAccount{
                UserID = dbUser.UserID,
                Balance = user.FreeAccountBalance,
                AccID = AccountIDGenerator.GenerateAccountID()
            };
            FreeAccounts.Add(freeAccount);
            SavingAccounts.Add(new DBSavingAccount{
                UserID = dbUser.UserID,
                Balance = user.SavingAccountBalance,
                Student = user.Student,
                DailyWithdrawal = 0,
                AccID = AccountIDGenerator.GenerateAccountID()
            });
            // Maturity date is set to 3 months from now
            CreditAccounts.Add(new DBCreditAccount{
                UserID = dbUser.UserID,
                Balance = user.CreditAccountBalance,
                MaturityDate = DateTime.Now.AddMonths(3),
                AccID = AccountIDGenerator.GenerateAccountID()
            });

            SaveChanges();
        }
    }
    
    public void ChangeRole(int userID, Role role, int adminID){
        DBUser? admin = Users.FirstOrDefault(u => u.UserID == adminID);
        if (admin == null || admin.Role != Role.Admin){
            throw new Exception("Admin not found");
        }
        DBUser? user = Users.FirstOrDefault(u => u.UserID == userID);
        if (user == null){
            throw new Exception("User not found");
        }
        user.Role = role;
        _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has been promoted to {role} by {admin.FirstName} {admin.LastName} ({admin.Email})");
        SaveChanges();
    }

    public void DeleteUser(int userID){
        DBUser? user = Users.FirstOrDefault(u => u.UserID == userID);
        if (user == null){
            throw new Exception("User not found");
        }
        Users.Remove(user);
        // Remove user's accounts
        FreeAccounts.RemoveRange(FreeAccounts.Where(a => a.UserID == userID));
        SavingAccounts.RemoveRange(SavingAccounts.Where(a => a.UserID == userID));
        CreditAccounts.RemoveRange(CreditAccounts.Where(a => a.UserID == userID));
        _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has been deleted");
        SaveChanges();
    }

    public DBUser? Login(string email, string password){
        DBUser? user = Users.FirstOrDefault(u => u.Email == email);
        if (user == null){
            return null;
        }
        if (!PasswordService.VerifyPassword(user.PasswordHash, password)){
            return null;
        }
        return user;
    }

    public void CheckLoans(){
        // Gets all credit accounts with negative balance and maturity date in the past
        var overdueLoans = CreditAccounts.Where(a => a.Balance < 0 && a.MaturityDate < DateTime.Now);
        foreach(DBCreditAccount account in overdueLoans){
            // Get user and check for his free account, if there is not enough money, check for saving account, if there is not enough money, bankrupt the user
            DBUser user = Users.First(u => u.UserID == account.UserID);
            DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == account.UserID);
            DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == account.UserID);
            if (freeAccount != null && freeAccount.Balance + account.Balance < 0){
                if (savingAccount != null && savingAccount.Balance + freeAccount.Balance + account.Balance < 0){
                    user.Bankrupt = true;
                    _logger.Error($"User {user.FirstName} {user.LastName} with email {user.Email} has gone bankrupt");
                }
                else{
                    if (savingAccount != null)
                    {
                        savingAccount.Balance += freeAccount.Balance + account.Balance;
                        freeAccount.Balance = 0;
                        user.Bankrupt = false;
                        _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has paid his loan from saving account");
                    }
                    else{
                        // If user has no saving account, log that
                        _logger.Error($"User {user.FirstName} {user.LastName} with email {user.Email} has no saving account");
                    }
                }
            }
            else{
                if (freeAccount != null)
                {
                    freeAccount.Balance += account.Balance;
                    user.Bankrupt = false;
                    _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has paid his loan from free account");
                }
                else{
                    // If user has no free account, log that
                    _logger.Error($"User {user.FirstName} {user.LastName} with email {user.Email} has no free account");
                }
            }
            account.Balance = 0;
            // Set maturity date to 1 months from now again
            account.MaturityDate = DateTime.Now.AddMonths(1);
        }
        SaveChanges();
    }

    public void CheckSavings(){
        // Get all saving accounts
        var savingAccounts = SavingAccounts.ToList();
        foreach(DBSavingAccount account in savingAccounts){
            // For all saving accounts add interest rate to balance
            account.Balance += account.Balance * (decimal)_interestRate;
        }
        SaveChanges();
        _logger.Information($"Interest rates have been added to {savingAccounts.Count} saving accounts");
    }

    public void LogMoneyTransfer(int userID, int srcAccID, int destAccID, AccountType srcAccType, AccountType destAccType, decimal amount, bool success){
        DBLog log = new DBLog{
            UserID = userID,
            SrcAccID = srcAccID,
            DestAccID = destAccID,
            SrcAccType = srcAccType,
            DestAccType = destAccType,
            Amount = amount,
            Time = DateTime.Now,
            Success = success
        };
        Logs.Add(log);
        SaveChanges();
        if (success){
            _logger.Information($"User {userID} has transferred {amount} from {srcAccID} to {destAccID}");
        }
        else{
            _logger.Error($"User {userID} has tried to transfer {amount} from {srcAccID} to {destAccID} but failed");
        }
    }

    public bool CheckAccountExistence(int accountID, AccountType accountType){
        switch(accountType){
            case AccountType.Free:
                return FreeAccounts.Any(a => a.AccID == accountID);
            case AccountType.Saving:
                return SavingAccounts.Any(a => a.AccID == accountID);
            case AccountType.Credit:
                return CreditAccounts.Any(a => a.AccID == accountID);
            default:
                return false;
        }
    }
    
    public void TransferMoney(int UserID, string source, string destination, decimal amount){
        // Example account number format: 123456/001 -> 123456 is UserID, 001 is account type (1 - Free, 2 - Saving, 3 - Credit)
        int srcAccID = int.Parse(source.Split('/')[0]);
        int destAccID = int.Parse(destination.Split('/')[0]);
        
        string srcAccType = source.Split('/')[1];
        string destAccType = destination.Split('/')[1];

        AccountType srcAccountType = srcAccType switch{
            "001" => AccountType.Free,
            "002" => AccountType.Saving,
            "003" => AccountType.Credit,
            _ => AccountType.Free
        };
        AccountType destAccountType = destAccType switch{
            "001" => AccountType.Free,
            "002" => AccountType.Saving,
            "003" => AccountType.Credit,
            _ => AccountType.Free
        };

        // Check if source and destination accounts exist
        if (!CheckAccountExistence(srcAccID, srcAccountType)){
            _logger.Error($"Source account {srcAccID} does not exist");
            LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
            throw new Exception("Account does not exist");
        }
        if(!CheckAccountExistence(destAccID, destAccountType)){
            _logger.Error($"Destination account {destAccID} does not exist");
            LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
            throw new Exception("Account does not exist");
        }

        // Get source and destination accounts
        var srcAcc = srcAccountType switch{
            AccountType.Free => FreeAccounts.First(a => a.AccID == srcAccID),
            AccountType.Saving => SavingAccounts.First(a => a.AccID == srcAccID),
            AccountType.Credit => CreditAccounts.First(a => a.AccID == srcAccID),
            _ => null
        };
        var destAcc = destAccountType switch{
            AccountType.Free => FreeAccounts.First(a => a.AccID == destAccID),
            AccountType.Saving => SavingAccounts.First(a => a.AccID == destAccID),
            AccountType.Credit => CreditAccounts.First(a => a.AccID == destAccID),
            _ => null
        };

        if (srcAcc == null || destAcc == null){
            _logger.Fatal($"Something went wrong and source or destination account is null");
            LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
            throw new Exception("Account does not exist");
        }

        // If source is student saving account, check if daily withdrawal limit is exceeded or if withdrawal limit is exceeded
        if (srcAccountType == AccountType.Saving && srcAcc is DBSavingAccount studentAccount){
            if (studentAccount.Student){
                if (amount > _maxStudentWithdrawal){
                    _logger.Error($"User {UserID} has tried to withdraw {amount} from his student saving but exceeded withdrawal limit");
                    LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
                    throw new Exception("Student saving account daily withdrawal limit exceeded");
                }
                if (studentAccount.DailyWithdrawal + amount > _maxStudentDailyWithdrawal){
                    _logger.Error($"User {UserID} has tried to withdraw {amount} from his student saving but exceeded daily withdrawal limit");
                    LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
                    throw new Exception("Student saving account withdrawal limit exceeded");
                }
                studentAccount.DailyWithdrawal += amount;
            }
        }

        // Check if source account has enough money, if not, check if user has credit account and if he can take a loan
        if (srcAcc.Balance < amount){
            if (srcAccountType == AccountType.Credit){
                if (srcAcc.Balance + _maxDebt < amount){
                    _logger.Error($"User {UserID} has tried to transfer {amount} from his credit account but exceeded maximum debt");
                    LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
                    throw new Exception("Not enough money in credit account");
                }
                // If user has credit account, take a loan
                srcAcc.Balance -= amount;
                destAcc.Balance += amount;
                LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, true);
                SaveChanges();
                _logger.Information($"User {UserID} has taken a loan of {amount} from his credit account");
                return;
            }
            else{
                _logger.Error($"User {UserID} has tried to transfer {amount} from his {srcAccType} account but exceeded balance");
                LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, false);
                throw new Exception("Not enough money in account");
            }
        }

        srcAcc.Balance -= amount;
        destAcc.Balance += amount;

        LogMoneyTransfer(UserID, srcAccID, destAccID, srcAccountType, destAccountType, amount, true);
        _logger.Information($"User {UserID} has transferred {amount} from {srcAccID} to {destAccID}");
        SaveChanges();

    }

    public User? GetUser(int userID){
        DBUser? dbUser = Users.FirstOrDefault(u => u.UserID == userID);
        if (dbUser == null){
            return null;
        }
        if (dbUser.Role == Role.User){
            DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == userID);
            DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == userID);
            DBCreditAccount? creditAccount = CreditAccounts.FirstOrDefault(a => a.UserID == userID);
            return new User(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID, freeAccount != null ? new FreeAccount(freeAccount.AccID, freeAccount.UserID, freeAccount.Balance) : null, savingAccount != null ? new SavingAccount(savingAccount.AccID, savingAccount.UserID, savingAccount.Balance, savingAccount.Student, savingAccount.DailyWithdrawal) : null, creditAccount != null ? new CreditAccount(creditAccount.AccID, creditAccount.UserID, creditAccount.Balance, creditAccount.MaturityDate) : null, dbUser.Bankrupt);
        }
        else if (dbUser.Role == Role.Banker){
            return new Banker(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID);
        }
        else if (dbUser.Role == Role.Admin){
            return new Admin(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID);
        }
        return null;
    }
    
    public DBUser? GetUserByEmail(string email){
        DBUser? dbUser = Users.FirstOrDefault(u => u.Email == email);
        if (dbUser == null){
            return null;
        }
        return dbUser;
    }

    public List<int> GetBalance(int userID){
        List<int> balances = new List<int>();
        DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == userID);
        DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == userID);
        DBCreditAccount? creditAccount = CreditAccounts.FirstOrDefault(a => a.UserID == userID);
        if (freeAccount != null){
            balances.Add((int)freeAccount.Balance);
        }
        else{
            balances.Add(0);
        }
        if (savingAccount != null){
            balances.Add((int)savingAccount.Balance);
        }
        else{
            balances.Add(0);
        }
        if (creditAccount != null){
            balances.Add((int)creditAccount.Balance);
        }
        else{
            balances.Add(0);
        }
        return balances;
    }

    public List<string> GetAccountIDs(int userID){
        List<string> accountIDs = new List<string>();
        DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == userID);
        DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == userID);
        DBCreditAccount? creditAccount = CreditAccounts.FirstOrDefault(a => a.UserID == userID);
        if (freeAccount != null){
            accountIDs.Add(freeAccount.AccID.ToString() + "/001");
        }
        if (savingAccount != null){
            accountIDs.Add(savingAccount.AccID.ToString() + "/002");
        }
        if (creditAccount != null){
            accountIDs.Add(creditAccount.AccID.ToString() + "/003");
        }
        return accountIDs;
    }

    public DBLog[] GetUsersLogs(int userID){
        return Logs.Where(l => l.UserID == userID).ToArray();
    }
    
    public DBLog[] GetAccountLogs(string account){
        int accountID = int.Parse(account.Split('/')[0]);
        string accountTypeNum = account.Split('/')[1];
        AccountType accountType = accountTypeNum switch
        {
            "001" => AccountType.Free,
            "002" => AccountType.Saving,
            "003" => AccountType.Credit,
            _ => AccountType.Free
        };
        // Get all logs where source or destination account ID and type match the given account ID and type
        return Logs.Where(l => (accountType == l.SrcAccType && l.SrcAccID == accountID) || (accountType == l.DestAccType && l.DestAccID == accountID)).ToArray();
    }
}
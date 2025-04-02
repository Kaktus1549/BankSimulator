using Microsoft.EntityFrameworkCore;
using Serilog.Debugging;
using System.Globalization;

public class BankaDB : DbContext
{
    public DbSet<DBUser> Users { get; set; }
    public DbSet<DBLog> Logs { get; set; }
    public DbSet<DBFreeAccount> FreeAccounts { get; set; }
    public DbSet<DBSavingAccount> SavingAccounts { get; set; }
    public DbSet<DBCreditAccount> CreditAccounts { get; set; }
    public DbSet<DBStatistics> Statistics { get; set; }

    private string _serverAddress;
    private string _databaseName;
    private string _username;
    private string _password;
    private int _port;
    private decimal _maxDebt;
    private decimal _maxStudentWithdrawal;
    private decimal _maxStudentDailyWithdrawal;
    private float _interestRate;
    private string _masterName;
    private string _masterLastName;
    private string _masterEmail;
    private string _masterPassword;
    private readonly Serilog.ILogger _logger;

    public BankaDB(DbContextOptions<BankaDB> options, 
                IConfiguration configuration, 
                Serilog.ILogger logger) 
        : base(options)
    {
        // Read configuration values
        _serverAddress = configuration["DB_SERVER_ADDRESS"] ?? throw new Exception("DB_SERVER_ADDRESS is required");
        _databaseName = configuration["DB_NAME"] ?? throw new Exception("DB_NAME is required");
        _username = configuration["DB_USERNAME"] ?? throw new Exception("DB_USERNAME is required");
        _password = configuration["DB_PASSWORD"] ?? throw new Exception("DB_PASSWORD is required");
        _port = int.Parse(configuration["DB_SERVER_PORT"] ?? "3306");
        _maxDebt = decimal.Parse(configuration["MAX_DEBT"] ?? "10000", CultureInfo.InvariantCulture);
        _maxStudentWithdrawal = decimal.Parse(configuration["MAX_STUDENT_WITHDRAWAL"] ?? "2000", CultureInfo.InvariantCulture);
        _maxStudentDailyWithdrawal = decimal.Parse(configuration["MAX_STUDENT_DAILY_WITHDRAWAL"] ?? "4000", CultureInfo.InvariantCulture);
        _interestRate = float.Parse(configuration["INTEREST_RATE"] ?? "0.05", CultureInfo.InvariantCulture);
        _masterName = configuration["MASTER_NAME"] ?? "Kaktus";
        _masterLastName = configuration["MASTER_LAST_NAME"] ?? "1549";
        _masterEmail = configuration["MASTER_EMAIL"] ?? "admin@kaktusgame.eu";
        _masterPassword = configuration["MASTER_PASSWORD"] ?? "admin";
        _logger = logger;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){
        string connectionString = $"Server={_serverAddress};Port={_port};Database={_databaseName};Uid={_username};Pwd={_password};";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), 
                                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
        _logger.Information("Database connection established!");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DBUser>().ToTable("users");
        modelBuilder.Entity<DBLog>().ToTable("logs");
        modelBuilder.Entity<DBFreeAccount>().ToTable("free_accounts");
        modelBuilder.Entity<DBSavingAccount>().ToTable("saving_accounts");
        modelBuilder.Entity<DBCreditAccount>().ToTable("credit_accounts");
        modelBuilder.Entity<DBStatistics>().ToTable("statistics");
    }

    public async Task EnsureMaster(){
        if (!Users.Any(u => u.Email == _masterEmail)){
            var master = new DBUser{
                FirstName = _masterName,
                LastName = _masterLastName,
                Email = _masterEmail,
                PasswordHash = PasswordService.HashPassword(_masterPassword),
                Role = Role.Admin,
                Bankrupt = false
            };
            Users.Add(master);
            await SaveChangesAsync();
            _logger.Information($"Master user {_masterName} {_masterLastName} with email {_masterEmail} has been created");
        }
        await Task.CompletedTask;
    }
    
    public Task AddUser(RegisterDTO user){
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
                AccID = AccountIDGenerator.GenerateAccountID(),
                BalanceHistory = new decimal[30]
            };
            FreeAccounts.Add(freeAccount);
            SavingAccounts.Add(new DBSavingAccount{
                UserID = dbUser.UserID,
                Balance = user.SavingAccountBalance,
                Student = user.Student,
                DailyWithdrawal = 0,
                AccID = AccountIDGenerator.GenerateAccountID(),
                BalanceHistory = new decimal[30],
                MonthlyHistory = new decimal[12]
            });
            // Maturity date is set to 3 months from now
            CreditAccounts.Add(new DBCreditAccount{
                UserID = dbUser.UserID,
                Balance = user.CreditAccountBalance,
                MaturityDate = DateTime.Now.AddMonths(3),
                AccID = AccountIDGenerator.GenerateAccountID(),
                BalanceHistory = new decimal[30]
            });

            SaveChanges();
        }
        return Task.CompletedTask;
    }
    
    public Task ChangeRole(int userID, Role role, int adminID){
        DBUser? admin = Users.FirstOrDefault(u => u.UserID == adminID);
        if (admin == null || admin.Role != Role.Admin){
            throw new Exception("Admin not found");
        }
        DBUser? user = Users.FirstOrDefault(u => u.UserID == userID);
        if (role == Role.User){
            // Check if user has free account, saving account and credit account, if not, create them empty
            DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == userID);
            DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == userID);
            DBCreditAccount? creditAccount = CreditAccounts.FirstOrDefault(a => a.UserID == userID);
            if (freeAccount == null){
                freeAccount = new DBFreeAccount{
                    UserID = userID,
                    Balance = 0,
                    AccID = AccountIDGenerator.GenerateAccountID(),
                    BalanceHistory = new decimal[30]
                };
                FreeAccounts.Add(freeAccount);
            }
            if (savingAccount == null){
                savingAccount = new DBSavingAccount{
                    UserID = userID,
                    Balance = 0,
                    Student = false,
                    DailyWithdrawal = 0,
                    AccID = AccountIDGenerator.GenerateAccountID(),
                    BalanceHistory = new decimal[30],
                    MonthlyHistory = new decimal[12]
                };
                SavingAccounts.Add(savingAccount);
            }
            if (creditAccount == null){
                creditAccount = new DBCreditAccount{
                    UserID = userID,
                    Balance = 0,
                    MaturityDate = DateTime.Now.AddMonths(3),
                    AccID = AccountIDGenerator.GenerateAccountID(),
                    BalanceHistory = new decimal[30]
                };
                CreditAccounts.Add(creditAccount);
            }
            SaveChanges();
        }
        if (user == null){
            throw new Exception("User not found");
        }
        user.Role = role;
        _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has been promoted to {role} by {admin.FirstName} {admin.LastName} ({admin.Email})");
        SaveChanges();
        return Task.CompletedTask;
    }

    public Task DeleteUser(int userID){
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
        return Task.CompletedTask;
    }

    public Task<DBUser?> Login(string email, string password){
        DBUser? user = Users.FirstOrDefault(u => u.Email == email);
        if (user == null){
            return Task.FromResult<DBUser?>(null);
        }
        if (!PasswordService.VerifyPassword(user.PasswordHash, password)){
            return Task.FromResult<DBUser?>(null);
        }
        return Task.FromResult<DBUser?>(user);
    }

    public Task CheckLoans(){
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
        return Task.CompletedTask;
    }

    public Task CheckSavings(){
        // Get all saving accounts
        var savingAccounts = SavingAccounts.ToList();
        foreach(DBSavingAccount account in savingAccounts){
            // Get dailyhistory, for each day divide by 30 and add it to the balance
            decimal bal = 0;
            if (account.BalanceHistory != null){
                for (int i = 0; i < account.BalanceHistory.Length; i++){
                    bal += account.BalanceHistory[i] / 30;
                }
                bal *= (decimal)_interestRate / 12;

                account.Balance += bal;
            }
        }
        SaveChanges();
        _logger.Information($"Interest rates have been added to {savingAccounts.Count} saving accounts");
        return Task.CompletedTask;
    }

    public Task LogMoneyTransfer(int userID, int srcAccID, int destAccID, AccountType srcAccType, AccountType destAccType, decimal amount, bool success){
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
        return Task.CompletedTask;
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
    
    public Task TransferMoney(int UserID, string source, string destination, decimal amount){
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
            AccountType.Free => FreeAccounts.Where(a => a.AccID == srcAccID ).FirstOrDefault(),
            AccountType.Saving => SavingAccounts.Where(a => a.AccID == srcAccID ).FirstOrDefault(),
            AccountType.Credit => CreditAccounts.Where(a => a.AccID == srcAccID ).FirstOrDefault(),
            _ => null
        };
        var destAcc = destAccountType switch{
            AccountType.Free => FreeAccounts.Where(a => a.AccID == destAccID ).FirstOrDefault(),
            AccountType.Saving => SavingAccounts.Where(a => a.AccID == destAccID ).FirstOrDefault(),
            AccountType.Credit => CreditAccounts.Where(a => a.AccID == destAccID ).FirstOrDefault(),
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
                return Task.CompletedTask;
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
        return Task.CompletedTask;
    }

    public Task<User?> GetUser(int userID){
        DBUser? dbUser = Users.FirstOrDefault(u => u.UserID == userID);
        if (dbUser == null){
            return Task.FromResult<User?>(null);
        }
        if (dbUser.Role == Role.User){
            DBFreeAccount? freeAccount = FreeAccounts
                .Where(a => a.UserID == userID )
                .FirstOrDefault();
            DBSavingAccount? savingAccount = SavingAccounts
                .Where(a => a.UserID == userID )
                .FirstOrDefault();
            DBCreditAccount? creditAccount = CreditAccounts
                .Where(a => a.UserID == userID )
                .FirstOrDefault();
            return Task.FromResult<User?>(new User(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID, freeAccount != null ? new FreeAccount(freeAccount.AccID, freeAccount.UserID, freeAccount.Balance) : null, savingAccount != null ? new SavingAccount(savingAccount.AccID, savingAccount.UserID, savingAccount.Balance, savingAccount.Student, savingAccount.DailyWithdrawal) : null, creditAccount != null ? new CreditAccount(creditAccount.AccID, creditAccount.UserID, creditAccount.Balance, creditAccount.MaturityDate) : null, dbUser.Bankrupt));
        }
        else if (dbUser.Role == Role.Banker){
            return Task.FromResult<User?>(new Banker(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID));
        }
        else if (dbUser.Role == Role.Admin){
            return Task.FromResult<User?>(new Admin(dbUser.FirstName, dbUser.LastName, dbUser.Email, dbUser.UserID));
        }
        return Task.FromResult<User?>(null);
    }
    
    public Task<DBUser?> GetUserByEmail(string email){
        DBUser? dbUser = Users.FirstOrDefault(u => u.Email == email);
        if (dbUser == null){
            return Task.FromResult<DBUser?>(null);
        }
        return Task.FromResult<DBUser?>(dbUser);
    }

    public Task<List<int>> GetBalance(int userID){
        DBFreeAccount? freeAccount = FreeAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();
        DBSavingAccount? savingAccount = SavingAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();
        DBCreditAccount? creditAccount = CreditAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();

        var balances = new List<int>
        {
            freeAccount != null ? (int)freeAccount.Balance : 0,
            savingAccount != null ? (int)savingAccount.Balance : 0,
            creditAccount != null ? (int)creditAccount.Balance : 0
        };

        if (freeAccount == null)
            _logger.Error($"User {userID} has no free account");
        if (savingAccount == null)
            _logger.Error($"User {userID} has no saving account");
        if (creditAccount == null)
            _logger.Error($"User {userID} has no credit account");

        return Task.FromResult(balances);
    }

    public Task<List<string>> GetAccountIDs(int userID){
        // Check if user is admin or banker, if yes return undefined
        DBUser? user = Users.FirstOrDefault(u => u.UserID == userID);
        if (user == null){
            throw new Exception("User not found");
        }
        if (user.Role == Role.Admin || user.Role == Role.Banker){
            return Task.FromResult<List<string>>(new List<string> { "undefined" });
        }
        List<string> accountIDs = new List<string>();
        DBFreeAccount? freeAccount = FreeAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();
        DBSavingAccount? savingAccount = SavingAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();
        DBCreditAccount? creditAccount = CreditAccounts
            .Where(a => a.UserID == userID )
            .FirstOrDefault();
        if (freeAccount != null){
            accountIDs.Add(freeAccount.AccID.ToString() + "/001");
        }
        if (savingAccount != null){
            accountIDs.Add(savingAccount.AccID.ToString() + "/002");
            accountIDs.Add(savingAccount.Student ? "Student" : "Non-student");
        }
        if (creditAccount != null){
            accountIDs.Add(creditAccount.AccID.ToString() + "/003");
        }

        if (freeAccount == null || savingAccount == null || creditAccount == null){
            throw new Exception("User has some accounts null");
        }
        return Task.FromResult<List<string>>(accountIDs);
    }

    public Task<DBLog[]> GetUsersLogs(int userID){
        return Task.FromResult<DBLog[]>(Logs.Where(l => l.UserID == userID).ToArray());
    }
    
    public Task<DBLog[]> GetAccountLogs(string account){
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
        return  Task.FromResult<DBLog[]>(Logs.Where(l => (accountType == l.SrcAccType && l.SrcAccID == accountID) || (accountType == l.DestAccType && l.DestAccID == accountID)).ToArray());
    }

    public Task<AllLogs[]> GetLogs(){
        // Get all logs and join them with users table to get first and last name of the user
        var logs = Logs.Join(Users, l => l.UserID, u => u.UserID, (l, u) => new AllLogs{
            FirstName = u.FirstName,
            LastName = u.LastName,
            email = u.Email,
            SrcAcc = l.SrcAccID.ToString() + "/" + (l.SrcAccType == AccountType.Free ? "001" : l.SrcAccType == AccountType.Saving ? "002" : "003"),
            DestAcc = l.DestAccID.ToString() + "/" + (l.DestAccType == AccountType.Free ? "001" : l.DestAccType == AccountType.Saving ? "002" : "003"),
            Amount = l.Amount,
            Time = l.Time,
            Success = l.Success
        }).ToArray();
        return Task.FromResult<AllLogs[]>(logs);
    }

    public Task UpdateDailyHistory(){
        // Get all accounts and update their daily history
        var FreeAccount = FreeAccounts.ToList();
        var SavingAccount = SavingAccounts.ToList();
        var CreditAccount = CreditAccounts.ToList();

        // Update daily history for free accounts based on the current balance
        // BalanceHistory is array of 30 elements, representing the last 30 days
        // First element is today, last element is 30 days ago
        // We need shift the array and set the first element to the current balance
        foreach (var account in FreeAccount){
            DateTime lastUpdated = account.BalanceLastUpdated;
            // Check if last updated date is today
            if (lastUpdated.Date == DateTime.Now.Date){
                // If last updated date is today, do not update the balance history
                continue;
            }
            if (account.BalanceHistory == null){
                account.BalanceHistory = new decimal[30];
            }
            // Shift the array to the right
            for (int i = 29; i > 0; i--){
                account.BalanceHistory[i] = account.BalanceHistory[i - 1];
            }
            account.BalanceHistory[0] = account.Balance;
            // Update last updated date
            account.BalanceLastUpdated = DateTime.Now;
        }

        foreach (var account in SavingAccount){
            DateTime lastUpdated = account.BalanceLastUpdated;
            // Check if last updated date is today
            if (lastUpdated.Date == DateTime.Now.Date){
                // If last updated date is today, do not update the balance history
                continue;
            }
            if (account.BalanceHistory == null){
                account.BalanceHistory = new decimal[30];
            }
            // Shift the array to the right
            for (int i = 29; i > 0; i--){
                account.BalanceHistory[i] = account.BalanceHistory[i - 1];
            }
            account.BalanceHistory[0] = account.Balance;
            // Update last updated date
            account.BalanceLastUpdated = DateTime.Now;
        }

        foreach (var account in CreditAccount){
            DateTime lastUpdated = account.BalanceLastUpdated;
            // Check if last updated date is today
            if (lastUpdated.Date == DateTime.Now.Date){
                // If last updated date is today, do not update the balance history
                continue;
            }
            if (account.BalanceHistory == null){
                account.BalanceHistory = new decimal[30];
            }
            // Shift the array to the right
            for (int i = 29; i > 0; i--){
                account.BalanceHistory[i] = account.BalanceHistory[i - 1];
            }
            account.BalanceHistory[0] = account.Balance;
            // Update last updated date
            account.BalanceLastUpdated = DateTime.Now;
        }
        // Save changes to the database
        SaveChanges();
        _logger.Information($"Daily history has been updated for {FreeAccount.Count + SavingAccount.Count + CreditAccount.Count} accounts");
        return Task.CompletedTask;
    }

    public Task UpdateMonthlyHistory(){
        // Get all accounts and update their monthly history
        var SavingAccount = SavingAccounts.ToList();
        // Update monthly history for saving accounts based on the current balance
        // MonthlyHistory is array of 12 elements, representing the last 12 months
        // First element is this month, last element is 12 months ago
        // We need shift the array and set the first element to the current balance
        foreach (var account in SavingAccount){
            DateTime lastUpdated = account.MonthlyHistoryLastUpdated;
            // Check if last updated date is this month
            if (lastUpdated.Year == DateTime.Now.Year && lastUpdated.Month == DateTime.Now.Month){
                // If last updated date is this month, do not update the monthly history
                continue;
            }
            if (account.MonthlyHistory == null){
                account.MonthlyHistory = new decimal[12];
            }
            // Shift the array to the right
            for (int i = 11; i > 0; i--){
                account.MonthlyHistory[i] = account.MonthlyHistory[i - 1];
            }
            account.MonthlyHistory[0] = account.Balance;
            // Update last updated date
            account.MonthlyHistoryLastUpdated = DateTime.Now;
        }
        // Save changes to the database
        SaveChanges();
        _logger.Information($"Monthly history has been updated for {SavingAccount.Count} accounts");
        return Task.CompletedTask;
    }

    public Task<Decimal[]> GetMonthlyHistory(string email){
        // Get user by email
        DBUser? user = Users.FirstOrDefault(u => u.Email == email);
        if (user == null){
            throw new Exception("User not found");
        }
        if (user.Role != Role.User){
            throw new Exception("Admin or banker cannot have monthly history");
        }
        // Get saving account for user
        DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == user.UserID);
        if (savingAccount == null){
            throw new Exception("Saving account not found");
        }
        return Task.FromResult<Decimal[]>(savingAccount.MonthlyHistory ?? new decimal[12]);
    }

    public Task<Decimal[]> GetDailyHistory(string email){
        // Get user by email
        DBUser? user = Users.FirstOrDefault(u => u.Email == email);
        if (user == null){
            throw new Exception("User not found");
        }
        if (user.Role != Role.User){
            throw new Exception("Admin or banker cannot have daily history");
        }
        // Get free account for user
        DBFreeAccount? freeAccount = FreeAccounts.FirstOrDefault(a => a.UserID == user.UserID);
        if (freeAccount == null){
            throw new Exception("Free account not found");
        }
        return Task.FromResult<Decimal[]>(freeAccount.BalanceHistory ?? new decimal[30]);
    }

    public Task<User?> GetUserByEmailOrAccount(string searchTerm){
        DBUser? user;
        if (searchTerm.Contains("@")){
            user = Users.FirstOrDefault(u => u.Email == searchTerm);
        }
        else{
            // Get user by account number
            int accountID = int.Parse(searchTerm.Split('/')[0]);
            string accountTypeNum = searchTerm.Split('/')[1];
            AccountType accountType = accountTypeNum switch
            {
                "001" => AccountType.Free,
                "002" => AccountType.Saving,
                "003" => AccountType.Credit,
                _ => AccountType.Free
            };
            var account = CheckAccountExistence(accountID, accountType);
            if (!account){
                throw new Exception("Account not found");
            }
            user = Users.FirstOrDefault(u => u.UserID == accountID);
        }
        if (user == null){
            throw new Exception("User not found");
        }

        if (user.Role == Role.User){
            DBFreeAccount? freeAccount = FreeAccounts
                .Where(a => a.UserID == user.UserID)
                .FirstOrDefault();
            DBSavingAccount? savingAccount = SavingAccounts
                .Where(a => a.UserID == user.UserID)
                .FirstOrDefault();
            DBCreditAccount? creditAccount = CreditAccounts
                .Where(a => a.UserID == user.UserID)
                .FirstOrDefault();
            _logger.Information($"User {user.FirstName} {user.LastName} with email {user.Email} has been found");
            return Task.FromResult<User?>(new User(user.FirstName, user.LastName, user.Email, user.UserID, freeAccount != null ? new FreeAccount(freeAccount.AccID, freeAccount.UserID, freeAccount.Balance) : null, savingAccount != null ? new SavingAccount(savingAccount.AccID, savingAccount.UserID, savingAccount.Balance, savingAccount.Student, savingAccount.DailyWithdrawal) : null, creditAccount != null ? new CreditAccount(creditAccount.AccID, creditAccount.UserID, creditAccount.Balance, creditAccount.MaturityDate) : null, user.Bankrupt));
        }
        else if (user.Role == Role.Banker){
            throw new Exception("Cant view banker account");
        }
        else if (user.Role == Role.Admin){
            throw new Exception("Cant view admin account");
        }
        else{
            throw new Exception("User not found");
        }
    }

    public Task UpdateStatistics(){
        // Get all users
        int totalUserCount = Users.Count();
        int totalAccountCount = FreeAccounts.Count() + SavingAccounts.Count() + CreditAccounts.Count();
        int freeAccountCount = FreeAccounts.Count();
        int savingAccountCount = SavingAccounts.Count();
        int creditAccountCount = CreditAccounts.Count();
        int totalTransactionCount = Logs.Count();
        int totalBankerCount = Users.Count(u => u.Role == Role.Banker);
        int totalAdminCount = Users.Count(u => u.Role == Role.Admin);
        int totalUsersCount = Users.Count(u => u.Role == Role.User);
        // Sum of all balances in Credit accounts which are negative
        decimal totalDebit = CreditAccounts.Sum(a => a.Balance < 0 ? a.Balance : 0);        
        

        // Update statistics, or create new one if it does not exist
        DBStatistics? statistics = Statistics.FirstOrDefault();
        if (statistics == null){
            decimal[] last30Days = new decimal[30];
            // Set all values to 0, first one is today, last one is 30 days ago
            // First one will be totalDebit

            for (int i = 0; i < last30Days.Length; i++){
                if (i == 0){
                    last30Days[i] = totalDebit;
                }
                else{
                    last30Days[i] = 0;
                }
            }
            statistics = new DBStatistics{
                TotalUserCount = totalUserCount,
                TotalAdminCount = totalAdminCount,
                TotalUsersCount = totalUsersCount,
                TotalBankerCount = totalBankerCount,
                TotalAccountCount = totalAccountCount,
                FreeAccountCount = freeAccountCount,
                SavingAccountCount = savingAccountCount,
                CreditAccountCount = creditAccountCount,
                TotalTransactionCount = totalTransactionCount,
                TotalDebt30Days = last30Days,
                LastUpdate = DateTime.Now,
                ID = 1 // Set ID to 1, since this is the first and only statistics
            };
            Statistics.Add(statistics);
        }
        else{
            decimal[] last30Days = statistics.TotalDebt30Days ?? new decimal[30];

            // Shift the array to the right
            for (int i = 29; i > 0; i--){
                last30Days[i] = last30Days[i - 1];
            }
            last30Days[0] = totalDebit;

            statistics.TotalUserCount = totalUserCount;
            statistics.TotalAdminCount = totalAdminCount;
            statistics.TotalUsersCount = totalUsersCount;
            statistics.TotalBankerCount = totalBankerCount;
            statistics.TotalAccountCount = totalAccountCount;
            statistics.FreeAccountCount = freeAccountCount;
            statistics.SavingAccountCount = savingAccountCount;
            statistics.CreditAccountCount = creditAccountCount;
            statistics.TotalTransactionCount = totalTransactionCount;
            statistics.TotalDebt30Days = last30Days;
            statistics.LastUpdate = DateTime.Now;
        }
        SaveChanges();
        
        return Task.FromResult(statistics);
    }
    public Task<DBStatistics?> GetStatistics(){
        DBStatistics? statistics = Statistics.FirstOrDefault();
        if (statistics == null){
            throw new Exception("Statistics not found");
        }
        return Task.FromResult<DBStatistics?>(statistics);
    }

    public Task ChangeStudentStatus(string email, bool isStudent){
        // Get user by email
        DBUser? user = Users.FirstOrDefault(u => u.Email == email);
        if (user == null){
            throw new Exception("User not found");
        }
        if (user.Role != Role.User){
            throw new Exception("Admin or banker cannot have student status");
        }
        // Get saving account for user
        DBSavingAccount? savingAccount = SavingAccounts.FirstOrDefault(a => a.UserID == user.UserID);
        if (savingAccount == null){
            throw new Exception("Saving account not found");
        }
        // Change student status
        savingAccount.Student = isStudent;
        SaveChanges();
        return Task.CompletedTask;
    }
}
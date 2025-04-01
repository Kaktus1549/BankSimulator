using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;

[Route("[controller]")]
[ApiController]
public class apiController : ControllerBase
{

    private readonly Serilog.ILogger _logger;
    private string _jwtSecret;
    private string _issuer;
    private BankaDB _db;

    public apiController(Serilog.ILogger logger, Dictionary<string, string> configuration, BankaDB db)
    {
        _logger = logger;
        _jwtSecret = configuration["jwtSecret"] ?? throw new ArgumentNullException("jwtSecret");
        _issuer = configuration["issuer"] ?? throw new ArgumentNullException("issuer");
        _db = db;
    }

    public Dictionary<string, string>? ValidateUser(HttpRequest request){
        string token = request.Cookies["token"] ?? string.Empty;
        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress;
        
        if (remoteIpAddress == null){
            throw new Exception("IP address is required");
        }
        if (string.IsNullOrEmpty(token))
        {
            _logger.Information($"Address {remoteIpAddress} didn't provide a token.");
            return null;
        }
        string ipAddr = remoteIpAddress.ToString();

        if (!JWT.ValidateJwtToken(token, _jwtSecret, _issuer, ipAddr))
        {
            _logger.Information($"Address {remoteIpAddress} provided an invalid token.");
            return null;
        }

        var data = new Dictionary<string, string>();
        try{
            data = JWT.DecodeJWT(token);
            return data;
        }
        catch{
            _logger.Information($"Address {remoteIpAddress} provided an invalid token.");
            return null;
        }
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only admin or banker can register a user.");
        }

        // Banker can only register users
        if (data["role"] == "Banker" && model.Role != Role.User){
            return BadRequest("Banker can only register users.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        if (model.FreeAccountBalance < 0){
            return BadRequest("Free account balance cannot start at negative.");
        }
        if (model.SavingAccountBalance < 0){
            return BadRequest("Saving account balance cannot start at negative.");
        }
        if (model.CreditAccountBalance < 0){
            return BadRequest("Credit account balance cannot start at negative.");
        }
        

        try{
            await _db.AddUser(model);
            _logger.Information($"User {model.Email} registered successfully by {data["email"]} ({data["role"]}) from {Request.HttpContext.Connection.RemoteIpAddress}.");
            return Ok("User registered successfully.");
        }
        catch (Exception ex){
            _logger.Error($"Error registering user {model.Email}: {ex.Message}");
            return BadRequest(new { errors = new[] { $"Error registering user. {model.Email}: {ex.Message}" } });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model){
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        var user = await _db.Login(model.Email, model.Password);
        if (user == null){
            return BadRequest("User not found.");
        }
        var ipAddr = Request?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipAddr)){
            return BadRequest("IP address is required.");
        }
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("user_id", user.UserID.ToString());
        data.Add("email", user.Email);
        data.Add("role", user.Role.ToString());

        var token = JWT.GenerateJwtToken(_jwtSecret, _issuer, data, ipAddr);
        //Response.Cookies.Append("token", token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = true, Expires = DateTime.Now.AddHours(24) });
        Response.Cookies.Append("token", token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Expires = DateTime.Now.AddHours(24) });
        Dictionary<string, string> response = new Dictionary<string, string>();
        response.Add("email", user.Email);
        response.Add("role", user.Role.ToString());
        response.Add("firstName", user.FirstName);
        response.Add("lastName", user.LastName);
        _logger.Information($"User {user.Email} logged in successfully from {Request?.HttpContext.Connection.RemoteIpAddress}.");
        return Ok(response);
    }

    [HttpGet("logout")]
    public IActionResult Logout(){
        Response.Cookies.Delete("token");
        return Ok("Logged out successfully.");
    }

    [HttpGet("validate")]
    public async Task<IActionResult> Validate(){
        var data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        var user = await _db.GetUserByEmail(data["email"]);
        if (user == null){
            return BadRequest("User not found.");
        }
        data.Add("firstName", user.FirstName);
        data.Add("lastName", user.LastName);
        return Ok(data);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        int userId = int.Parse(data["user_id"]);
        var balance = await _db.GetBalance(userId);
        return Ok(balance);
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> GetAccounts(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        int userId = int.Parse(data["user_id"]);
        var accounts =  await _db.GetAccountIDs(userId);
        return Ok(accounts);
    }

    [HttpPost("chmod")]
    public async Task<IActionResult> ChangeRole([FromBody] ChangeRoleDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin"){
            return BadRequest("Only admin can change roles.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        DBUser? user = await _db.GetUserByEmail(model.Email);
        if (user == null){
            return BadRequest("User not found.");
        }
        await _db.ChangeRole(user.UserID, model.Role, int.Parse(data["user_id"]));
        _logger.Information($"Role of {model.Email} changed to {model.Role} by {data["email"]} ({data["role"]}) from {Request.HttpContext.Connection.RemoteIpAddress}.");
        return Ok("Role changed successfully.");
    }

    [HttpPost("delete")]
    public async Task<IActionResult> DeleteUser([FromBody] DeleteUserDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin"){
            return BadRequest("Only admin can delete users.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        DBUser? user = await _db.GetUserByEmail(model.Email);
        if (user == null){
            return BadRequest("User not found.");
        }
        await _db.DeleteUser(user.UserID);
        _logger.Information($"User {model.Email} deleted by {data["email"]} ({data["role"]}) from {Request.HttpContext.Connection.RemoteIpAddress}.");
        return Ok("User deleted successfully.");
    }

    [HttpPost("transactionsByUser")]
    public async Task<IActionResult> GetTransactionsByUser([FromBody] transactionsByUserDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin" && data["role"] != "Banker"){
            // Check if the user is trying to view their own transactions
            if (data["email"] != model.Email){
                return BadRequest("Only admin or banker can view transactions.");
            }
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        DBUser? user = await _db.GetUserByEmail(model.Email);
        if (user == null){
            return BadRequest("User not found.");
        }
        var transactions = _db.GetUsersLogs(user.UserID);
        return Ok(transactions);
    }

    // If someone wants for specific account
    [HttpPost("transactionsByAccount")]
    public IActionResult GetTransactionsByAccount([FromBody] transactionsByAccountDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only admin or banker can view transactions.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        var transactions = _db.GetAccountLogs(model.AccountNumber);
        return Ok(transactions);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] transferDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "User"){
            return BadRequest("Only users can transfer money.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        if (model.Amount <= 0){
            return BadRequest("Amount must be greater than 0.");
        }
        if (model.FromAccountNumber == model.ToAccountNumber){
            return BadRequest("Cannot transfer to the same account.");
        }
        try{
            await _db.TransferMoney(int.Parse(data["user_id"]), model.FromAccountNumber, model.ToAccountNumber, model.Amount);
            _logger.Information($"User {data["email"]} tried to transfer {model.Amount} from {model.FromAccountNumber} to {model.ToAccountNumber} from address {Request.HttpContext.Connection.RemoteIpAddress}.");
            return Ok("Transfer successful.");
        }
        catch (Exception ex){
            _logger.Error($"Error transferring money: {ex.Message}");
            return BadRequest(new { errors = new[] { $"Error transferring money: {ex.Message}" } });
        }
    }

    [HttpGet("getall")]
    public IActionResult GetAllLogs(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        _logger.Information($"User {data["email"]} requested all logs from {Request.HttpContext.Connection.RemoteIpAddress}.");
        _logger.Information($"User {data["role"]} requested all logs from {Request.HttpContext.Connection.RemoteIpAddress}.");
        if (data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only admin or banker can view logs.");
        }
        var logs = _db.GetLogs();
        return Ok(logs);
    }

    [HttpPost("getMonthly")]
    public IActionResult GetMonthlyHistory([FromBody] monthlyDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["email"] != model.Email && data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only bankers and admin can view other users' monthly history.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        try{
            var monthlyHistory = _db.GetMonthlyHistory(model.Email);
            return Ok(monthlyHistory);
        }
        catch (Exception ex){
            _logger.Error($"Error getting monthly history: {ex.Message}");
            return BadRequest(new { errors = new[] { $"Error getting monthly history: {ex.Message}" } });
        }
    }

    [HttpPost("getDaily")]
    public IActionResult GetDailyHistory([FromBody] dailyDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["email"] != model.Email && data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only bankers and admin can view other users' daily history.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        try{
            var dailyHistory = _db.GetDailyHistory(model.Email);
            return Ok(dailyHistory);
        }
        catch (Exception ex){
            _logger.Error($"Error getting daily history: {ex.Message}");
            return BadRequest(new { errors = new[] { $"Error getting daily history: {ex.Message}" } });
        }
    }

    [HttpPost("view")]
    public async Task<IActionResult> Search([FromBody] SearchDTO model){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin" && data["role"] != "Banker"){
            return BadRequest("Only admin or banker can view users.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        try{
            var user = await _db.GetUserByEmailOrAccount(model.SearchTerm);
            if (user == null){
                return BadRequest("User not found.");
            }
            _logger.Information($"User {data["email"]} searched for {model.SearchTerm} from {Request.HttpContext.Connection.RemoteIpAddress}.");
            // return array of two arrays, first one is account numbers, second one is account balances
            List<List<string>> accounts = new List<List<string>>();
            List<string> accountNumbers = user.GetAccountNumbers();
            List<string> accountBalances = user.GetAccountBalances().Select(x => x.ToString()).ToList();
            accounts.Add(accountNumbers);
            accounts.Add(accountBalances);
            var response = new {
                name = user.GetName(),
                email = user.GetEmail(),
                accounts = accounts
            };
            
            return Ok(response);
        }
        catch (Exception ex){
            _logger.Error($"Error searching for user: {ex.Message}");
            return BadRequest(new { errors = new[] { $"Error searching for user: {ex.Message}" } });
        }
    }

    [HttpGet("adminStats")]
    public async Task<IActionResult> GetAdminStats(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin"){
            return BadRequest("Only admin can view stats.");
        }
        var stats = await _db.GetStatistics();
        // Return number of users, admins, bankers, update time
        if (stats == null){
            return BadRequest("Error getting stats.");
        }
        var response = new {
            TotalUsersCount = stats?.TotalUsersCount,
            TotalAdminCount = stats?.TotalAdminCount,
            TotalBankerCount = stats?.TotalBankerCount,
            LastUpdate = stats?.LastUpdate
        };
        return Ok(response);
    }

    [HttpGet("bankerStats")]
    public async Task<IActionResult> GetBankerStats(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Banker" && data["role"] != "Admin"){
            return BadRequest("Only banker or admin can view these stats");
        }
        var stats = await _db.GetStatistics();
        // Return list of debt in 30 days, number of Free, Saving, Credit accounts
        if (stats == null){
            return BadRequest("Error getting stats.");
        }
        var response = new {
            TotalDebt30Days = stats?.TotalDebt30Days,
            FreeAccountCount = stats?.FreeAccountCount,
            SavingAccountCount = stats?.SavingAccountCount,
            CreditAccountCount = stats?.CreditAccountCount,
            LastUpdate = stats?.LastUpdate
        };
        return Ok(response);
    }

    [HttpGet("allStats")]
    // For API scraping etc
    public async Task<IActionResult> GetAllStats(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        if (data["role"] != "Admin"){
            return BadRequest("Only admin can view stats.");
        }
        var stats = await _db.GetStatistics();
        // Return all stats
        if (stats == null){
            return BadRequest("Error getting stats.");
        }
        return Ok(stats);
    }
}
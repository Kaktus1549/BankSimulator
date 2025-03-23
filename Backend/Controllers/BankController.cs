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
        

        await Task.Run(() => _db.AddUser(model));
        _logger.Information($"User {model.Email} registered successfully by {data["email"]} ({data["role"]}) from {Request.HttpContext.Connection.RemoteIpAddress}.");
        return Ok("User registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO model){
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        var user = await Task.Run(() => _db.Login(model.Email, model.Password));
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
        Response.Cookies.Append("token", token, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict, Secure = true, Expires = DateTime.Now.AddHours(24) });
        return Ok("Logged in successfully.");
    }

    [HttpGet("logout")]
    public IActionResult Logout(){
        Response.Cookies.Delete("token");
        return Ok("Logged out successfully.");
    }

    [HttpGet("validate")]
    public IActionResult Validate(){
        var data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        return Ok(data);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        int userId = int.Parse(data["user_id"]);
        var balance = await Task.Run(() => _db.GetBalance(userId));
        return Ok(balance);
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> GetAccounts(){
        Dictionary<string, string>? data = ValidateUser(Request);
        if (data == null){
            return BadRequest("Invalid token.");
        }
        int userId = int.Parse(data["user_id"]);
        var accounts = await Task.Run(() => _db.GetAccountIDs(userId));
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
        DBUser? user = await Task.Run(() => _db.GetUserByEmail(model.Email));
        if (user == null){
            return BadRequest("User not found.");
        }
        await Task.Run(() => _db.ChangeRole(user.UserID, model.Role, int.Parse(data["user_id"])));
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
        DBUser? user = await Task.Run(() => _db.GetUserByEmail(model.Email));
        if (user == null){
            return BadRequest("User not found.");
        }
        await Task.Run(() => _db.DeleteUser(user.UserID));
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
            return BadRequest("Only admin or banker can view transactions.");
        }
        if (!ModelState.IsValid){
            return BadRequest(ModelState);
        }
        DBUser? user = await Task.Run(() => _db.GetUserByEmail(model.Email));
        if (user == null){
            return BadRequest("User not found.");
        }
        var transactions = await Task.Run(() => _db.GetUsersLogs(user.UserID));
        return Ok(transactions);
    }

    [HttpPost("transactionsByAccount")]
    public async Task<IActionResult> GetTransactionsByAccount([FromBody] transactionsByAccountDTO model){
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
        var transactions = await Task.Run(() => _db.GetAccountLogs(model.AccountNumber));
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
        
        await Task.Run(() => _db.TransferMoney(int.Parse(data["user_id"]), model.FromAccountNumber, model.ToAccountNumber, model.Amount));
        _logger.Information($"User {data["email"]} tried to transfer {model.Amount} from {model.FromAccountNumber} to {model.ToAccountNumber} from address {Request.HttpContext.Connection.RemoteIpAddress}.");
        return Ok("Transfer successful.");
    }
}
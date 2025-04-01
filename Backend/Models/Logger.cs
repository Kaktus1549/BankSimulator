using Serilog.Core;
using Serilog.Events;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class DBLog{
    [Key]
    public int LogID { get; set; }

    [ForeignKey("UserID")]    
    public DBUser? User { get; set; }
    public int UserID { get; set; }

    [ForeignKey("SrcAccID")]
    public DBFreeAccount? SrcAcc { get; set; }
    public int SrcAccID { get; set; }
    [ForeignKey("DestAccID")]
    public DBFreeAccount? DestAcc { get; set; }
    public int DestAccID { get; set; }
    public AccountType SrcAccType { get; set; }
    public AccountType DestAccType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Time { get; set; }
    public bool Success { get; set; }
}

public class AllLogs{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? email { get; set; }
    public string? SrcAcc { get; set; }
    public string? DestAcc { get; set; }
    public decimal Amount { get; set; }
    public DateTime Time { get; set; }
    public bool Success { get; set; }
}

public class DiscordSink : ILogEventSink
{
    private readonly string _webhookUrl;
    private readonly string _username;
    private readonly string? _avatar;
    private readonly IFormatProvider? _formatProvider;
    private static readonly HttpClient _httpClient = new();
    private bool _enabled = true;

    public DiscordSink(string webhookUrl, string username, string? avatar = null, IFormatProvider? formatProvider = null, bool enabled = true){
        _webhookUrl = webhookUrl;
        _username = username;
        _avatar = avatar;
        _formatProvider = formatProvider;
        _enabled = enabled;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Information) return; // Skip Debug/Verbose
        if (!_enabled) return; // Skip if disabled

        var message = logEvent.RenderMessage(_formatProvider);
        var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        var final = $"```[{timestamp}] [{logEvent.Level}] {message}```";

        var payload = new Dictionary<string, string>
        {
            ["username"] = _username,
            ["content"] = final
        };
        if (!string.IsNullOrEmpty(_avatar))
            payload["avatar_url"] = _avatar;

        // Fire and forget (non-blocking)
        _ = _httpClient.PostAsync(_webhookUrl, new FormUrlEncodedContent(payload));
    }
}
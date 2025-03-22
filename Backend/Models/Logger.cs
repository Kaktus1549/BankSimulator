public class DBLog{
    public int LogID { get; set; }
    public int UserID { get; set; }
    public int SrcAccID { get; set; }
    public int DestAccID { get; set; }
    public AccountType SrcAccType { get; set; }
    public AccountType DestAccType { get; set; }
    public decimal Amount { get; set; }
    public DateTime Time { get; set; }
    public bool Success { get; set; }
}

public class LoggerHelper{
    private string _WebhookURL;
    private string _WebhookUsername = "Logger";
    private string? _WebhookProfilePicture;
    private bool _Enabled = true;

    public LoggerHelper(string WebhookURL, string WebhookUsername, string? WebhookProfilePicture, bool Enabled){
        _WebhookURL = WebhookURL;
        _WebhookUsername = WebhookUsername;
        _WebhookProfilePicture = WebhookProfilePicture;
        _Enabled = Enabled;
    }
    public async Task SendWebhookLog(string message){
        if(_Enabled){
            using(HttpClient client = new HttpClient()){
                Dictionary<string, string> data = new Dictionary<string, string>
                {
                    { "username", _WebhookUsername },
                    { "content", message }
                };
                if (_WebhookProfilePicture != null)
                {
                    data["avatar_url"] = _WebhookProfilePicture;
                }
                FormUrlEncodedContent content = new FormUrlEncodedContent(data);
                try{
                    await client.PostAsync(_WebhookURL, content);
                }
                catch(Exception e){
                    Console.WriteLine(e.Message);
                }
            }
        }
    }
    public static DBLog[] FilterLogsByUser(int UserID, DBLog[] logs){
        List<DBLog> filteredLogs = new List<DBLog>();
        foreach(DBLog log in logs){
            if(log.UserID == UserID){
                filteredLogs.Add(log);
            }
        }
        return filteredLogs.ToArray();
    }
    public static DBLog[] FilterLogsByAccount(int AccountID, DBLog[] logs){
        List<DBLog> filteredLogs = new List<DBLog>();
        foreach(DBLog log in logs){
            if(log.SrcAccID == AccountID || log.DestAccID == AccountID){
                filteredLogs.Add(log);
            }
        }
        return filteredLogs.ToArray();
    }
}
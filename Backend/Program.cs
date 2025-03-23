using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using DotNetEnv;
using System.Security.Cryptography;
using System.Globalization;


namespace BankBackend{
    public class Program{

        public static string Generate256BitKey()
        {
            // Generate 32 bytes (256 bits) of random data
            byte[] key = new byte[32];
            RandomNumberGenerator.Fill(key);

            string secret = Convert.ToBase64String(key);

            // Save the generated key to the .env file
            File.AppendAllText(".env", $"\nJWT_SECRET=\"{secret}\"");

            // Convert the key to a base64 string
            return secret;
        }

        public static void Main(string[] args){
            Env.Load();
            var outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level,-14}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(
                    outputTemplate: outputTemplate,
                    theme: new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
                    {
                        [ConsoleThemeStyle.Text] = "\x1b[90m",                 // grey
                        [ConsoleThemeStyle.LevelDebug] = "\x1b[34m",           // blue
                        [ConsoleThemeStyle.LevelInformation] = "\x1b[34m",     // blue
                        [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",         // yellow
                        [ConsoleThemeStyle.LevelError] = "\x1b[31m",           // red
                        [ConsoleThemeStyle.LevelFatal] = "\x1b[31m",           // red
                        [ConsoleThemeStyle.SecondaryText] = "\x1b[32m",        // green
                    })
                )
                .WriteTo.File(
                    "logs/log-.txt",
                    outputTemplate: outputTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7
                )
                .WriteTo.Sink(new DiscordSink(
                    webhookUrl: "https://discord.com/api/webhooks/1353294040455577661/yro5I25TZFsSth9GzPxYvDsTFEd84vos9jGp6aGf1E70C12JaHGP_hHxuqCrOAsU9NSF",
                    username: "xX_Testing_Xx",
                    avatar: "https://example.com/avatar.png"
                ))
                .CreateLogger();

            Dictionary<string, string> config = new Dictionary<string, string>
            {
                {"serverAddress", Env.GetString("DB_SERVER_ADDRESS")},
                {"serverPort", Env.GetString("DB_SERVER_PORT")},
                {"databaseName", Env.GetString("DB_NAME")},
                {"username", Env.GetString("DB_USERNAME")},
                {"password", Env.GetString("DB_PASSWORD")},
                // If JWT_SECRET generate and store a new secret 
                {"jwtSecret", Env.GetString("JWT_SECRET") ?? Generate256BitKey()},
                {"issuer", Env.GetString("JWT_ISSUER")},
                {"maxDebt", Env.GetString("MAX_DEBT")},
                {"maxStudentWithdrawal", Env.GetString("MAX_STUDENT_WITHDRAWAL")},
                {"maxStudentDailyWithdrawal", Env.GetString("MAX_STUDENT_DAILY_WITHDRAWAL")},
                {"interestRate", Env.GetString("INTEREST_RATE")}
            };

            Log.Information("Trying to connect to the database...");
            var bankaDB = new BankaDB(
                config["serverAddress"],
                config["databaseName"],
                config["username"],
                config["password"],
                Log.Logger,
                maxDebt: decimal.TryParse(config["maxDebt"], NumberStyles.Number, CultureInfo.InvariantCulture, out var md) ? md : 10000m,
                maxStudentWithdrawal: decimal.TryParse(config["maxStudentWithdrawal"], NumberStyles.Number, CultureInfo.InvariantCulture, out var msw) ? msw : 2000m,
                maxStudentDailyWithdrawal: decimal.TryParse(config["maxStudentDailyWithdrawal"], NumberStyles.Number, CultureInfo.InvariantCulture, out var msdw) ? msdw : 4000m,
                interestRate: float.TryParse(config["interestRate"], NumberStyles.Float, CultureInfo.InvariantCulture, out var ir) ? ir : 0.05f,
                port: int.TryParse(config["serverPort"], out var port) ? port : 3306
            );

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            builder.Services.AddSingleton(bankaDB);
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
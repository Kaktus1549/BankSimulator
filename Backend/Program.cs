using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using DotNetEnv;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;


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
                {"interestRate", Env.GetString("INTEREST_RATE")},
                {"masterName", Env.GetString("MASTER_NAME")},
                {"masterLastName", Env.GetString("MASTER_LAST_NAME")},
                {"masterEmail", Env.GetString("MASTER_EMAIL")},
                {"masterPassword", Env.GetString("MASTER_PASSWORD")}
            };

            Log.Information("Trying to connect to the database...");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            builder.Services.AddHostedService<DailyTaskService>();

            builder.Services.AddDbContext<BankaDB>(options =>
            {
                // Retrieve values from the environment or configuration
                var serverAddress = Env.GetString("DB_SERVER_ADDRESS");
                var serverPort = Env.GetString("DB_SERVER_PORT");
                var databaseName = Env.GetString("DB_NAME");
                var username = Env.GetString("DB_USERNAME");
                var password = Env.GetString("DB_PASSWORD");

                // Build the connection string
                var connectionString = $"Server={serverAddress};Port={serverPort};Database={databaseName};User Id={username};Password={password};";

                // Use the MySQL provider with auto-detection of the server version
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options => {
                options.AddPolicy("AllowBlazorClient",
                    policy => policy
                        .WithOrigins("http://localhost:5001","http://localhost:5002", "http://banka.kaktusgame.eu") // Replace with your client URL
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            var app = builder.Build();
            app.UseCors("AllowBlazorClient");

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
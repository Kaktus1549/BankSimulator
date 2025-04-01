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
                        [ConsoleThemeStyle.Text] = "\x1b[90m",                 
                        [ConsoleThemeStyle.LevelDebug] = "\x1b[34m",           
                        [ConsoleThemeStyle.LevelInformation] = "\x1b[34m",     
                        [ConsoleThemeStyle.LevelWarning] = "\x1b[33m",         
                        [ConsoleThemeStyle.LevelError] = "\x1b[31m",           
                        [ConsoleThemeStyle.LevelFatal] = "\x1b[31m",           
                        [ConsoleThemeStyle.SecondaryText] = "\x1b[32m",        
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
                var serverAddress = Env.GetString("DB_SERVER_ADDRESS");
                var serverPort = Env.GetString("DB_SERVER_PORT");
                var databaseName = Env.GetString("DB_NAME");
                var username = Env.GetString("DB_USERNAME");
                var password = Env.GetString("DB_PASSWORD");

                var connectionString = $"Server={serverAddress};Port={serverPort};Database={databaseName};User Id={username};Password={password};";
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            });

            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options => {
                options.AddPolicy("AllowBlazorClient",
                    policy => policy
                        .WithOrigins("http://localhost:5001", "http://localhost:5002", "http://banka.kaktusgame.eu")
                        .AllowAnyHeader()
                        .AllowAnyMethod());
            });

            var app = builder.Build();
            app.UseCors("AllowBlazorClient");

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseAuthorization();
            app.MapControllers();

            // Apply any pending migrations on startup (creates DB/tables if not present)
            using (var scope = app.Services.CreateScope())
            {
                int counter = 0;
                while(counter < 5){
                    try
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<BankaDB>();
                        dbContext.Database.Migrate();
                        Log.Information("Database migration completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while migrating the database.");
                        Log.Error("Retrying in 5 seconds...");
                        Thread.Sleep(5000); // Wait for 5 seconds before retrying
                    }
                    counter++;
                }
            }

            app.Run();
        }
    }
}

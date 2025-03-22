using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

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
    .CreateLogger();
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

// Add services to the container.
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

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

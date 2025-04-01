using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class DailyTaskService : BackgroundService
{
    private readonly ILogger<DailyTaskService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DailyTaskService(ILogger<DailyTaskService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Executing daily tasks...");

                // Create a new scope to obtain a BankaDB instance (which is scoped)
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<BankaDB>();

                    await MonthlyTasks(db);
                    await DailyTasks(db);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing daily tasks");
            }

            // Wait 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private Task MonthlyTasks(BankaDB db)
    {
        // Check if it's the first day of the month
        if (DateTime.Now.Day == 1)
        {
            _logger.LogInformation("It's the first day of the month! Increasing savings account interest rates.");
            db.CheckSavings();
            _logger.LogInformation("Savings account interest rates increased.");

            _logger.LogInformation("Updating monthly history for all accounts.");
            db.UpdateMonthlyHistory();
            _logger.LogInformation("Monthly history updated.");
        }
        return Task.CompletedTask;
    }

    private Task DailyTasks(BankaDB db)
    {
        _logger.LogInformation("Checking for loans that are due today.");
        db.CheckLoans();
        _logger.LogInformation("Loans checked.");

        _logger.LogInformation("Updating daily history for all accounts.");
        db.UpdateDailyHistory();
        _logger.LogInformation("Daily history updated.");

        _logger.LogInformation("Updating statistics...");
        db.UpdateStatistics();
        _logger.LogInformation("Statistics updated.");
        return Task.CompletedTask;
    }
}

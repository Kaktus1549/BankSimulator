using Serilog;

public class DailyTaskService : BackgroundService{
    private readonly ILogger<DailyTaskService> _logger;
    private BankaDB _db;

    public DailyTaskService(ILogger<DailyTaskService> logger, BankaDB db){
            _logger = logger;
            _db = db;
        }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        while (!stoppingToken.IsCancellationRequested){
            try{
                _logger.LogInformation("Executing daily tasks...");

                await RunFirstTask();
                await RunSecondTask();

            }
            catch (Exception ex){
                _logger.LogError(ex, "Error while executing daily tasks");
            }

            // Wait 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private Task RunFirstTask()
        {
            // Check if it's the first day of the month
            if (DateTime.Now.Day == 1)
            {
                // Replace with thy actual logic
                _logger.LogInformation($"It's the first day of the month! Increasing savings account interest rates.");
                _db.CheckSavings();
                _logger.LogInformation("Savings account interest rates increased.");
            }
            return Task.CompletedTask;
        }

        private Task RunSecondTask()
        {
            // Replace with thy actual logic
            _logger.LogInformation("Chcecking for loans that are due today.");
            _db.CheckLoans();
            _logger.LogInformation("Loans checked.");
            return Task.CompletedTask;
        }
}
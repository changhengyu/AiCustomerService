namespace AiCustomerService.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at: {Time}", DateTimeOffset.Now);
        // 占位实现：后续阶段会接入 Hangfire 等后台任务
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

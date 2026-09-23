namespace AuraLiving.Services;

public class InstagramRefreshHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InstagramRefreshHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);

    public InstagramRefreshHostedService(
        IServiceProvider serviceProvider,
        ILogger<InstagramRefreshHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InstagramRefreshHostedService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var instagramService = scope.ServiceProvider.GetRequiredService<IInstagramService>();
                await instagramService.RefreshFeedCacheAsync();
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown request
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Instagram feed background auto-refresh.");
            }
        }

        _logger.LogInformation("InstagramRefreshHostedService stopped.");
    }
}

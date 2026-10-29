namespace FarmAI.Services;

// Safe fallback notification generation: data comes from approved plans, never from AI.
public class AlertWorker(IServiceScopeFactory scopeFactory,ILogger<AlertWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope=scopeFactory.CreateScope();
                var created=await scope.ServiceProvider.GetRequiredService<OperationsService>().RefreshAlerts();
                if(created>0)log.LogInformation("Generated {Count} vaccination notifications",created);
            }
            catch(Exception ex){log.LogWarning(ex,"Alert refresh unsuccessful; will retry on next cycle.");}
            try{await Task.Delay(TimeSpan.FromHours(1),stoppingToken);}
            catch(OperationCanceledException){break;}
        }
    }
}

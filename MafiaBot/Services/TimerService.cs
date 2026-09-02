namespace MafiaBot.Services;
using System.Timers;

public class TimerService
{
    private TaskCompletionSource<long?> _completionSource = new ();
    private readonly ILogger<TimerService> _logger;
    
    public TimerService(ILogger<TimerService> logger)
    {
        _logger = logger;
    }

    public async Task<long?> StartTimer(TimeSpan timeout)
    {
        _logger.LogInformation($"Timer started for {timeout} seconds");
        _completionSource = new TaskCompletionSource<long?>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        using var cts = new CancellationTokenSource(timeout);

        using (cts.Token.Register(() => _completionSource.TrySetResult(null)))
        {
            return await _completionSource.Task;
        }
    }

    public void StopTimer(long? choice)
    {
        _logger.LogInformation($"Timer stopped");
        _completionSource.TrySetResult(choice);
    }
    
}
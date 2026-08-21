namespace MafiaBot.Services;
using System.Timers;

public class TimerService
{
    private readonly Timer _timer = new Timer();
    private readonly ILogger<TimerService> _logger;
    public delegate Task OnTimerEndHandler();
    private OnTimerEndHandler _callback;
    
    public TimerService(ILogger<TimerService> logger)
    {
        _logger = logger;
        _timer.Elapsed += OnTimedEvent;
    }

    public void StartTimer(OnTimerEndHandler callback, int time)
    {
        _timer.Stop();
        _timer.Interval = time*1000;
        _callback = callback;
        _timer.Start();
        
        _logger.LogInformation($"Timer started for {time} seconds");
    }

    public void StopTimer()
    {
        _timer.Stop();
        
        _logger.LogInformation($"Timer stopped");
    }
    
    private void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        _timer.Stop();
        _callback.Invoke();
        
        _logger.LogInformation($"Timer ended for {_timer.Interval/1000} seconds");
    }
}
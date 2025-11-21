using System.Diagnostics;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Options;
using DiningPhilosophers.Core.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Core.Hosting;

public sealed class PhilosopherHostedService : BackgroundService
{
    private readonly PhilosopherSeat _seat;
    private readonly IPhilosopherStrategy _strategy;
    private readonly ITableManager _tableManager;
    private readonly IMetricsCollector _metrics;
    private readonly IOptions<SimulationOptions> _options;
    private readonly ILogger<PhilosopherHostedService> _logger;
    private readonly Random _random;

    public PhilosopherHostedService(
        PhilosopherSeat seat,
        IPhilosopherStrategy strategy,
        ITableManager tableManager,
        IMetricsCollector metrics,
        IOptions<SimulationOptions> options,
        ILogger<PhilosopherHostedService> logger)
    {
        _seat = seat;
        _strategy = strategy;
        _tableManager = tableManager;
        _metrics = metrics;
        _options = options;
        _logger = logger;
        _random = new Random(Guid.NewGuid().GetHashCode());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = _options.Value;
        _logger.LogInformation("{Philosopher} hosted service started.", _seat.Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            int thinkTime = _random.Next(settings.ThinkingTimeMin, settings.ThinkingTimeMax + 1);
            _logger.LogDebug("{Philosopher} thinking for {Duration} ms", _seat.Name, thinkTime);
            await Task.Delay(thinkTime, stoppingToken);
            _metrics.RecordThinking(_seat.Name, thinkTime);

            bool acquired = false;
            var waitStart = Stopwatch.GetTimestamp();

            while (!stoppingToken.IsCancellationRequested)
            {
                acquired = _strategy.TryAcquireForks(_seat, _tableManager);
                if (acquired)
                {
                    break;
                }

                _metrics.RecordFailedAttempt(_seat.Name);
                await Task.Delay(settings.ForkAcquisitionTime, stoppingToken);
            }

            if (acquired)
            {
                var waitedMs = ElapsedMilliseconds(waitStart);
                if (waitedMs > 0)
                {
                    _metrics.RecordWaiting(_seat.Name, waitedMs);
                }
            }

            if (!acquired)
            {
                break;
            }

            try
            {
                _tableManager.MarkEating(_seat.Name, _seat.LeftForkIndex, _seat.RightForkIndex);
                int eatTime = _random.Next(settings.EatingTimeMin, settings.EatingTimeMax + 1);
                _logger.LogDebug("{Philosopher} eating for {Duration} ms", _seat.Name, eatTime);
                await Task.Delay(eatTime, stoppingToken);
                _metrics.RecordEating(_seat.Name, eatTime);
            }
            finally
            {
                _tableManager.ReleaseForks(_seat.Name, _seat.LeftForkIndex, _seat.RightForkIndex);
            }
        }

        _logger.LogInformation("{Philosopher} stopped.", _seat.Name);
    }

    private static long ElapsedMilliseconds(long startTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
        if (elapsedTicks <= 0)
        {
            return 0;
        }

        return (long)(elapsedTicks * 1000.0 / Stopwatch.Frequency);
    }
}

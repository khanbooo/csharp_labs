using System.IO;
using System.Text;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Options;
using DiningPhilosophers.Core.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Core.Hosting;

public sealed class StatusReporter : BackgroundService
{
    private readonly ITableManager _tableManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly IOptions<SimulationOptions> _options;
    private readonly SimulationOutput _output;
    private readonly ILogger<StatusReporter> _logger;

    public StatusReporter(
        ITableManager tableManager,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        SimulationOutput output,
        ILogger<StatusReporter> logger)
    {
        _tableManager = tableManager;
        _metricsCollector = metricsCollector;
        _options = options;
        _output = output;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = Math.Max(100, _options.Value.DisplayUpdateInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                WriteSnapshot();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write status snapshot.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void WriteSnapshot()
    {
        var report = _metricsCollector.BuildReport(_tableManager.GetStatus());
        var sb = new StringBuilder();
        sb.AppendLine($"===== TIME {DateTime.Now:HH:mm:ss.fff} =====");
        sb.AppendLine("Philosophers:");
        foreach (var philosopher in report.Philosophers)
        {
            sb.AppendLine(
                $" {philosopher.Name}: eaten={philosopher.EatingCount}, thinking={philosopher.TotalThinkingMs} ms, eatingTime={philosopher.TotalEatingMs} ms, failedAttempts={philosopher.FailedAttempts}");
        }

        sb.AppendLine();
        sb.AppendLine("Forks:");
        foreach (var fork in report.Forks)
        {
            sb.AppendLine($" Fork-{fork.Index + 1}: {fork.State} {(fork.Owner is null ? string.Empty : $"(is using by {fork.Owner})")}");
        }
        sb.AppendLine();

        var snapshot = sb.ToString();
        lock (_output.SyncRoot)
        {
            File.AppendAllText(_output.OutputPath, snapshot);
        }
    }
}

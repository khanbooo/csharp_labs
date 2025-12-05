using System.Collections.Generic;
using System.IO;
using System.Text;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Options;
using DiningPhilosophers.Core.Services;
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
    private readonly IReadOnlyList<PhilosopherSeat> _seats;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<StatusReporter> _logger;
    private bool _deadlockDetected;

    public StatusReporter(
        ITableManager tableManager,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        SimulationOutput output,
        IReadOnlyList<PhilosopherSeat> seats,
        IHostApplicationLifetime lifetime,
        ILogger<StatusReporter> logger)
    {
        _tableManager = tableManager;
        _metricsCollector = metricsCollector;
        _options = options;
        _output = output;
        _seats = seats;
        _lifetime = lifetime;
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
        var forkStatuses = _tableManager.GetStatus();
        var report = _metricsCollector.BuildReport(forkStatuses);
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

        CheckForDeadlock(report, forkStatuses);
    }

    private void CheckForDeadlock(SimulationReport report, IReadOnlyList<ForkStatus> forkStatuses)
    {
        if (_deadlockDetected || _seats.Count == 0)
        {
            return;
        }

        var snapshots = DeadlockSnapshotBuilder.BuildSnapshots(_seats, report, forkStatuses);
        if (snapshots.Count == 0)
        {
            return;
        }

        if (!DeadlockDetector.IsDeadlocked(snapshots))
        {
            return;
        }

        _deadlockDetected = true;
        var message = DeadlockSnapshotBuilder.BuildDeadlockMessage(snapshots);
        lock (_output.SyncRoot)
        {
            File.AppendAllText(_output.OutputPath, message);
        }
        _logger.LogWarning("Deadlock detected. Stopping application.");
        _lifetime.StopApplication();
    }

}

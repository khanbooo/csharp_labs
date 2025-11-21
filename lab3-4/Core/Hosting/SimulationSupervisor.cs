using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Options;
using DiningPhilosophers.Core.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiningPhilosophers.Core.Hosting;

public sealed class SimulationSupervisor : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ITableManager _tableManager;
    private readonly IMetricsCollector _metricsCollector;
    private readonly IOptions<SimulationOptions> _options;
    private readonly ILogger<SimulationSupervisor> _logger;
    private readonly SimulationOutput _output;
    private bool _summaryWritten;
    private readonly Stopwatch _simulationStopwatch = new();

    public SimulationSupervisor(
        IHostApplicationLifetime lifetime,
        ITableManager tableManager,
        IMetricsCollector metricsCollector,
        IOptions<SimulationOptions> options,
        SimulationOutput output,
        ILogger<SimulationSupervisor> logger)
    {
        _lifetime = lifetime;
        _tableManager = tableManager;
        _metricsCollector = metricsCollector;
        _options = options;
        _output = output;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        _logger.LogInformation("Simulation started for {Duration}s", options.DurationSeconds);

        _simulationStopwatch.Start();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(options.DurationSeconds), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Simulation supervisor cancelled.");
            return;
        }

        WriteSummary();
        _logger.LogInformation("Stopping host after simulation duration elapsed.");
        _lifetime.StopApplication();
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        WriteSummary();
        return base.StopAsync(cancellationToken);
    }

    private void WriteSummary()
    {
        if (_summaryWritten)
        {
            return;
        }

        if (_simulationStopwatch.IsRunning)
        {
            _simulationStopwatch.Stop();
        }

        var durationMs = _simulationStopwatch.Elapsed.TotalMilliseconds;
        var report = _metricsCollector.BuildReport(
            _tableManager.GetStatus(),
            _tableManager.GetUtilization(),
            durationMs);
        var sb = new StringBuilder();
        sb.AppendLine("==== Simulation Summary ====");
        sb.AppendLine($"Total thinking time (ms): {report.TotalThinkingMs}");
        sb.AppendLine($"Total eating time (ms): {report.TotalEatingMs}");
        sb.AppendLine();
        sb.AppendLine("Philosophers:");
        foreach (var philosopher in report.Philosophers)
        {
            sb.AppendLine(
                $" - {philosopher.Name}: thinking={philosopher.TotalThinkingMs} ms, eating={philosopher.TotalEatingMs} ms, eaten={philosopher.EatingCount}, failedAttempts={philosopher.FailedAttempts}");
        }

        sb.AppendLine();
        sb.AppendLine("Forks:");
        foreach (var fork in report.Forks)
        {
            sb.AppendLine($" - Fork {fork.Index + 1}: {fork.State} {(fork.Owner is null ? string.Empty : $"(owner: {fork.Owner})")}");
        }

        var culture = CultureInfo.CurrentCulture;
        var durationForThroughput = Math.Max(durationMs, 1);
        var totalMeals = report.Philosophers.Sum(p => p.EatingCount);
        var averageThroughput = report.Philosophers.Count == 0
            ? 0d
            : report.Philosophers.Sum(p => p.EatingCount / durationForThroughput) / report.Philosophers.Count;

        sb.AppendLine();
        sb.AppendLine("==== Final Metrics ====");
        sb.AppendLine($"Total eaten: {totalMeals}");
        sb.AppendLine("Throughput (items per ms):");
        foreach (var philosopher in report.Philosophers)
        {
            var throughput = philosopher.EatingCount / durationForThroughput;
            sb.AppendLine($" {philosopher.Name}: {throughput.ToString("F6", culture)}");
        }
        sb.AppendLine($" Average: {averageThroughput.ToString("F6", culture)}");

        sb.AppendLine("Waiting time (ms) - average per philosopher:");
        foreach (var philosopher in report.Philosophers)
        {
            sb.AppendLine($" {philosopher.Name}: {philosopher.TotalWaitingMs.ToString("F2", culture)} ms");
        }

        sb.AppendLine("Fork utilization (% of time):");
        foreach (var fork in report.ForkUtilization)
        {
            sb.AppendLine(
                $" Fork-{fork.ForkId}: Available={fork.AvailablePercent.ToString("F2", culture)}%, Queued={fork.QueuedPercent.ToString("F2", culture)}%, InUse={fork.InUsePercent.ToString("F2", culture)}%, Eating={fork.EatingPercent.ToString("F2", culture)}%");
        }

        var summary = sb.ToString();
        var directory = Path.GetDirectoryName(_output.OutputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        lock (_output.SyncRoot)
        {
            File.AppendAllText(_output.OutputPath, summary + Environment.NewLine);
        }
        _logger.LogInformation("Summary written to {Path}", _output.OutputPath);
        _summaryWritten = true;
    }
}

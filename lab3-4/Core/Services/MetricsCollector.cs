using System.Collections.Concurrent;
using System.Linq;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Utility;

namespace DiningPhilosophers.Core.Services;

public sealed class MetricsCollector : IMetricsCollector
{
    private readonly ConcurrentDictionary<string, PhilosopherSummary> _metrics = new();

    public void RegisterPhilosopher(string philosopherName)
    {
        _metrics.GetOrAdd(philosopherName, name => new PhilosopherSummary { Name = name });
    }

    public void RecordThinking(string philosopherName, int milliseconds)
    {
        var summary = _metrics.GetOrAdd(philosopherName, name => new PhilosopherSummary { Name = name });
        lock (summary)
        {
            summary.TotalThinkingMs += milliseconds;
        }
    }

    public void RecordEating(string philosopherName, int milliseconds)
    {
        var summary = _metrics.GetOrAdd(philosopherName, name => new PhilosopherSummary { Name = name });
        lock (summary)
        {
            summary.TotalEatingMs += milliseconds;
            summary.EatingCount += 1;
        }
    }

    public void RecordWaiting(string philosopherName, long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return;
        }

        var summary = _metrics.GetOrAdd(philosopherName, name => new PhilosopherSummary { Name = name });
        lock (summary)
        {
            summary.TotalWaitingMs += milliseconds;
        }
    }

    public void RecordFailedAttempt(string philosopherName)
    {
        var summary = _metrics.GetOrAdd(philosopherName, name => new PhilosopherSummary { Name = name });
        lock (summary)
        {
            summary.FailedAttempts += 1;
        }
    }

    public SimulationReport BuildReport(
        IReadOnlyList<ForkStatus> forkStatuses,
        IReadOnlyList<ForkUtilization>? forkUtilization = null,
        double? simulationDurationMs = null)
    {
        var ordered = _metrics.Values.OrderBy(s => s.Name).ToArray();
        var totalThinking = ordered.Sum(s => s.TotalThinkingMs);
        var totalEating = ordered.Sum(s => s.TotalEatingMs);

        return new SimulationReport
        {
            Philosophers = ordered,
            Forks = forkStatuses,
            ForkUtilization = forkUtilization ?? Array.Empty<ForkUtilization>(),
            TotalThinkingMs = totalThinking,
            TotalEatingMs = totalEating,
            SimulationDurationMs = simulationDurationMs ?? 0d
        };
    }
}

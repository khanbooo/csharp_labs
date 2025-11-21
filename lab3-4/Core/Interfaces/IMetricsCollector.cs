using DiningPhilosophers.Core.Utility;

namespace DiningPhilosophers.Core.Interfaces;

public interface IMetricsCollector
{
    void RegisterPhilosopher(string philosopherName);
    void RecordThinking(string philosopherName, int milliseconds);
    void RecordEating(string philosopherName, int milliseconds);
    void RecordWaiting(string philosopherName, long milliseconds);
    void RecordFailedAttempt(string philosopherName);
    SimulationReport BuildReport(
        IReadOnlyList<ForkStatus> forkStatuses,
        IReadOnlyList<ForkUtilization>? forkUtilization = null,
        double? simulationDurationMs = null);
}

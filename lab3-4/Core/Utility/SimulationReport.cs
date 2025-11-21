namespace DiningPhilosophers.Core.Utility;

public sealed class SimulationReport
{
    public required IReadOnlyList<PhilosopherSummary> Philosophers { get; init; }
    public required IReadOnlyList<ForkStatus> Forks { get; init; }
    public required IReadOnlyList<ForkUtilization> ForkUtilization { get; init; }
    public required long TotalThinkingMs { get; init; }
    public required long TotalEatingMs { get; init; }
    public required double SimulationDurationMs { get; init; }
}

public sealed class PhilosopherSummary
{
    public required string Name { get; init; }
    public long TotalThinkingMs { get; set; }
    public long TotalEatingMs { get; set; }
    public long TotalWaitingMs { get; set; }
    public int EatingCount { get; set; }
    public int FailedAttempts { get; set; }
}

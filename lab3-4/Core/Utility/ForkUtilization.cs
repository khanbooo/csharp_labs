namespace DiningPhilosophers.Core.Utility;

public sealed record ForkUtilization(
    int ForkId,
    double AvailablePercent,
    double QueuedPercent,
    double InUsePercent,
    double EatingPercent);

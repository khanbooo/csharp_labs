using System.Collections.ObjectModel;

namespace DiningPhilosophers.Core.Statistics
{
    public sealed class Statistics
    {
        public long Duration { get; init; }
        public string DurationUnit { get; init; } = string.Empty;
        public long TotalEaten { get; init; }
        public IReadOnlyDictionary<string, long> EatenPerPhilosopher { get; init; } = new ReadOnlyDictionary<string, long>(new Dictionary<string, long>());
        public IReadOnlyDictionary<string, double> ThroughputPerPhilosopher { get; init; } = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>());
        public string ThroughputUnit { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, double> WaitingPerPhilosopher { get; init; } = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>());
        public string WaitingUnit { get; init; } = string.Empty;
        public IReadOnlyList<ForkStatistics> ForkUtilization { get; init; } = Array.Empty<ForkStatistics>();
        public double AverageThroughput { get; init; }
    }

    public sealed class ForkStatistics
    {
        public int ForkId { get; init; }
        public double AvailablePercent { get; init; }
        public double QueuedPercent { get; init; }
        public double InUsePercent { get; init; }
        public double EatingPercent { get; init; }
    }
}

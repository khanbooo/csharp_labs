using System.Collections.ObjectModel;
using System.Text;
using DiningPhilosophers.Core.Entities;
using DiningPhilosophers.Core.Entities.Multithreaded;

namespace DiningPhilosophers.Core.Statistics
{
    /// <summary>
    /// Отвечает за построение отчётов и статистики симуляции.
    /// </summary>
    public sealed class StatisticsReporter
    {
        private readonly List<string> _names;
        private readonly List<Fork> _forks;
        private readonly List<Philosopher> _philosophers;

        public StatisticsReporter(List<string> names, List<Fork> forks, List<Philosopher> philosophers)
        {
            _names = names ?? throw new ArgumentNullException(nameof(names));
            _forks = forks ?? throw new ArgumentNullException(nameof(forks));
            _philosophers = philosophers ?? throw new ArgumentNullException(nameof(philosophers));
        }

        public string GetStatusBlock()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"===== TIME {DateTime.Now:HH:mm:ss.fff} =====");
            sb.AppendLine("Philosophers:");

            foreach (var name in _names)
            {
                var philosopher = _philosophers.FirstOrDefault(p => p.Name == name);
                if (philosopher != null)
                {
                    sb.AppendLine($" {name}: eaten={philosopher.EatenCount}");
                }
                else
                {
                    sb.AppendLine($" {name}: eaten=0");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Forks:");
            for (int i = 0; i < _forks.Count; i++)
            {
                var f = _forks[i];
                sb.AppendLine($" Fork-{i + 1}: {f.State} {(f.Owner != null ? "(is using by " + f.Owner + ")" : "")}");
            }
            return sb.ToString();
        }

        public Statistics BuildStatistics(long durationMs)
        {
            var eaten = new Dictionary<string, long>(_philosophers.Count);
            var throughput = new Dictionary<string, double>(_philosophers.Count);
            var waiting = new Dictionary<string, double>(_philosophers.Count);

            foreach (var philosopher in _philosophers)
            {
                eaten[philosopher.Name] = philosopher.EatenCount;
                double tp = durationMs == 0 ? 0.0 : philosopher.EatenCount / (double)durationMs;
                throughput[philosopher.Name] = tp;
                waiting[philosopher.Name] = philosopher.WaitingMilliseconds;
            }

            double averageThroughput = throughput.Count > 0 ? throughput.Values.Average() : 0.0;

            var forkStats = new List<ForkStatistics>(_forks.Count);
            for (int i = 0; i < _forks.Count; i++)
            {
                var f = _forks[i];
                forkStats.Add(new ForkStatistics
                {
                    ForkId = i + 1,
                    AvailablePercent = durationMs == 0 ? 0 : f.AvailableMs * 100.0 / durationMs,
                    QueuedPercent = durationMs == 0 ? 0 : f.QueuedMs * 100.0 / durationMs,
                    InUsePercent = durationMs == 0 ? 0 : f.InUseMs * 100.0 / durationMs,
                    EatingPercent = durationMs == 0 ? 0 : f.InEatingMs * 100.0 / durationMs
                });
            }

            return new Statistics
            {
                Duration = durationMs,
                DurationUnit = "ms",
                TotalEaten = eaten.Values.Sum(),
                EatenPerPhilosopher = new ReadOnlyDictionary<string, long>(eaten),
                ThroughputPerPhilosopher = new ReadOnlyDictionary<string, double>(throughput),
                ThroughputUnit = "items/ms",
                WaitingPerPhilosopher = new ReadOnlyDictionary<string, double>(waiting),
                WaitingUnit = "ms",
                ForkUtilization = forkStats,
                AverageThroughput = averageThroughput
            };
        }

        public string GetSummary(Statistics stats)
        {
            var sb = new StringBuilder();
            sb.AppendLine("==== Final Summary (multithreaded) ====");
            sb.AppendLine($"Total eaten: {stats.TotalEaten}");
            sb.AppendLine("Per philosopher:");
            foreach (var name in _names)
            {
                if (stats.EatenPerPhilosopher.TryGetValue(name, out var eaten))
                {
                    sb.AppendLine($" {name}: eaten={eaten}");
                }
            }
            sb.AppendLine();

            sb.AppendLine("==== Metrics ====");
            sb.AppendLine("Throughput (items per ms):");
            foreach (var name in _names)
            {
                if (stats.ThroughputPerPhilosopher.TryGetValue(name, out var tp))
                {
                    sb.AppendLine($" {name}: {tp:F6}");
                }
            }
            sb.AppendLine($" Average: {stats.AverageThroughput:F6}");

            sb.AppendLine("Waiting time (ms) - average per philosopher:");
            foreach (var name in _names)
            {
                if (stats.WaitingPerPhilosopher.TryGetValue(name, out var wait))
                {
                    sb.AppendLine($" {name}: {wait:F2} ms");
                }
            }

            sb.AppendLine("Fork utilization (% of time):");
            foreach (var fork in stats.ForkUtilization)
            {
                sb.AppendLine($" Fork-{fork.ForkId}: Available={fork.AvailablePercent:F2}%, Queued={fork.QueuedPercent:F2}%, InUse={fork.InUsePercent:F2}%, Eating={fork.EatingPercent:F2}%");
            }

            sb.AppendLine($"Score: {stats.TotalEaten}");
            return sb.ToString();
        }

        public string GetDeadlockDescription()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine("DEADLOCK DETECTED");
            sb.AppendLine();
            sb.AppendLine("All philosophers are hungry and each holds exactly one fork.");
            sb.AppendLine("No philosopher can proceed - the system is deadlocked!");
            sb.AppendLine();
            sb.AppendLine("Current state:");

            for (int i = 0; i < _philosophers.Count; i++)
            {
                var p = _philosophers[i];
                var view = p.AsView();
                string leftForkStatus = view.HasLeftFork ? "HELD" : "waiting";
                string rightForkStatus = view.HasRightFork ? "HELD" : "waiting";

                sb.AppendLine($"  {p.Name}: Left fork [{leftForkStatus}], Right fork [{rightForkStatus}]");
            }

            sb.AppendLine();
            sb.AppendLine("Simulation will terminate.");
            sb.AppendLine();

            return sb.ToString();
        }
    }
}

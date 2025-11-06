using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Threading;

namespace DiningPhilosophers.Core.Entities.Multithreaded
{
    public sealed class SimpleMonitor : SimulationThread, IMonitor
    {
        private readonly string _outputPath;
        private readonly Func<string> _statusProvider;
        private readonly Func<bool> _deadlockChecker;
        private readonly Func<string> _deadlockMessageProvider;
        private readonly CancellationTokenSource _simulationCts;
        private readonly int _intervalMs;

        public SimpleMonitor(
            string outputPath,
            Func<string> statusProvider,
            Func<bool> deadlockChecker,
            Func<string> deadlockMessageProvider,
            CancellationTokenSource simulationCts,
            int intervalMs,
            CancellationToken cancellation)
            : base(cancellation, "Simulation-Monitor")
        {
            if (intervalMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(intervalMs), intervalMs, "Interval must be non-negative.");
            }

            _outputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));
            _statusProvider = statusProvider ?? throw new ArgumentNullException(nameof(statusProvider));
            _deadlockChecker = deadlockChecker ?? throw new ArgumentNullException(nameof(deadlockChecker));
            _deadlockMessageProvider = deadlockMessageProvider ?? throw new ArgumentNullException(nameof(deadlockMessageProvider));
            _simulationCts = simulationCts ?? throw new ArgumentNullException(nameof(simulationCts));
            _intervalMs = intervalMs;
        }

        protected override void Execute()
        {
            while (!Cancellation.IsCancellationRequested)
            {
                File.AppendAllText(_outputPath, _statusProvider());

                if (_deadlockChecker())
                {
                    string deadlockMessage = _deadlockMessageProvider();
                    File.AppendAllText(_outputPath, deadlockMessage);
                    Console.WriteLine(deadlockMessage);

                    _simulationCts.Cancel();
                    break;
                }

                SleepWithCancellation(_intervalMs);
            }
        }
    }
}

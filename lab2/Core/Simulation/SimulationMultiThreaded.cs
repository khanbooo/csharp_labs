using DiningPhilosophers.Core.Entities;
using DiningPhilosophers.Core.Factories;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Statistics;
using DiningPhilosophers.Core.Utility;
using PhilosopherImpl = DiningPhilosophers.Core.Entities.Multithreaded.Philosopher;
using MonitorImpl = DiningPhilosophers.Core.Entities.Multithreaded.SimpleMonitor;

namespace DiningPhilosophers.Core.Simulation
{
    public class SimulationMultiThreaded : ISimulation
    {
        private readonly List<string> _names;
        private readonly List<Fork> _forks;
        private readonly string _output;
        private readonly PhilosopherFactory _philosopherFactory;

        private readonly CancellationTokenSource _cts = new();
        private List<PhilosopherImpl>? _philosophers;
        private MonitorImpl? _monitor;
        private StatisticsReporter? _reporter;
        private Statistics.Statistics? _statistics;
        private bool _deadlockDetected = false;

        // config
        private const int SIMULATION_MS = 30000; // total simulation duration
        private const int PRINT_MS = 150; // status print interval
        private const int THINK_MIN = 30;
        private const int THINK_MAX = 100;
        private const int EAT_MIN = 40;
        private const int EAT_MAX = 50;
        private const int TAKE_FORK_MS = 20;

        public SimulationMultiThreaded(string[] names, string output, IStrategy strategy)
        {
            _names = names.ToList();
            _output = output;

            var forkFactory = new ForkFactory();
            _forks = forkFactory.CreateMany(names.Length).ToList();

            _philosopherFactory = new PhilosopherFactory(
                strategy,
                THINK_MIN,
                THINK_MAX,
                EAT_MIN,
                EAT_MAX,
                TAKE_FORK_MS
            );
        }

        public void Run()
        {
            var startTime = Environment.TickCount64;
            _philosophers = new List<PhilosopherImpl>(_names.Count);

            for (int i = 0; i < _names.Count; i++)
            {
                var left = _forks[i];
                var right = _forks[(i + 1) % _forks.Count];
                var philosopher = _philosopherFactory.Create(i, _names[i], left, right, _cts.Token, startTime);
                _philosophers.Add((PhilosopherImpl)philosopher);
            }

            _reporter = new StatisticsReporter(_names, _forks, _philosophers);

            foreach (var philosopher in _philosophers)
            {
                philosopher.Start();
            }

            _monitor = new MonitorImpl(
                _output,
                () => _reporter.GetStatusBlock(),
                () => DeadlockDetector.IsDeadlocked(_philosophers),
                () => _reporter.GetDeadlockDescription(),
                _cts,
                PRINT_MS,
                _cts.Token
            );
            _monitor.Start();

            // Run for fixed duration or until deadlock
            try
            {
                _cts.Token.WaitHandle.WaitOne(SIMULATION_MS);
            }
            catch (ObjectDisposedException)
            {
                // CancellationTokenSource was disposed, ignore
            }

            // Check if cancellation was triggered by deadlock or timeout
            if (_cts.Token.IsCancellationRequested)
            {
                _deadlockDetected = true;
            }
            else
            {
                _cts.Cancel();
            }

            foreach (var philosopher in _philosophers)
            {
                philosopher.Join();
            }
            _monitor.Join();

            // Write final summary (only if no deadlock, as deadlock message already written)
            _statistics = _reporter.BuildStatistics(SIMULATION_MS);
            if (!_deadlockDetected)
            {
                File.AppendAllText(_output, _reporter.GetSummary(_statistics));
            }
        }

        public Statistics.Statistics GetStatistics()
        {
            return _statistics ?? throw new InvalidOperationException("Simulation has not been run yet.");
        }
    }
}

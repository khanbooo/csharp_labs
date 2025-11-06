using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.PhilosopherUtils;
using DiningPhilosophers.Core.Threading;

namespace DiningPhilosophers.Core.Entities.Multithreaded
{
    public sealed class PhilosopherView
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public PhilosopherState State { get; init; }
        public bool HasLeftFork { get; init; }
        public bool HasRightFork { get; init; }
        public Fork? LeftFork { get; init; }
        public Fork? RightFork { get; init; }
        public long EatenCount { get; init; }
        public long WaitingMilliseconds { get; init; }
    }

    public sealed class Philosopher : SimulationThread, IPhilosopher
    {
        private const int ForkAcquireTimeoutMs = 100;

        private readonly Fork _leftFork;
        private readonly Fork _rightFork;
        private readonly IStrategy _strategy;
        private readonly Random _rng;
        private readonly int _thinkMin;
        private readonly int _thinkMax;
        private readonly int _eatMin;
        private readonly int _eatMax;
        private readonly int _takeForkMs;

        private PhilosopherState _state;
        private bool _hasLeftFork;
        private bool _hasRightFork;
        private long _hungryStartTime;

        private long _waitingTotal;
        private long _eatenCount;

        public Philosopher(
            int id,
            string name,
            Fork leftFork,
            Fork rightFork,
            IStrategy strategy,
            CancellationToken cancellation,
            long startTime,
            int thinkMin,
            int thinkMax,
            int eatMin,
            int eatMax,
            int takeForkMs) : base(cancellation, $"Philosopher-{id}-{name}")
        {
            Id = id;
            Name = name;
            _leftFork = leftFork;
            _rightFork = rightFork;
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _rng = new Random(unchecked(id + (int)(startTime % int.MaxValue)));
            _thinkMin = thinkMin;
            _thinkMax = thinkMax;
            _eatMin = eatMin;
            _eatMax = eatMax;
            _takeForkMs = takeForkMs;

            _state = PhilosopherState.Thinking;
        }

        public int Id { get; }

        public string Name { get; }

        public long EatenCount => Interlocked.Read(ref _eatenCount);

        public long WaitingMilliseconds => Interlocked.Read(ref _waitingTotal);

        protected override void Execute()
        {
            while (!Cancellation.IsCancellationRequested)
            {
                var view = BuildView();
                var action = _strategy.Decide(view);

                ExecuteAction(action);
            }
            ReleaseAllForks();
        }

        private PhilosopherView BuildView()
        {
            return new PhilosopherView
            {
                Id = Id,
                Name = Name,
                State = _state,
                LeftFork = _leftFork,
                RightFork = _rightFork,
                HasLeftFork = _hasLeftFork,
                HasRightFork = _hasRightFork
            };
        }

        private void ExecuteAction(PhilosopherAction action)
        {
            switch (action)
            {
                case PhilosopherAction.None:
                    ProcessCurrentState();
                    break;

                case PhilosopherAction.TakeLeftFork:
                    TryTakeLeftFork();
                    break;

                case PhilosopherAction.TakeRightFork:
                    TryTakeRightFork();
                    break;

                case PhilosopherAction.ReleaseLeftFork:
                    ReleaseLeftFork();
                    break;

                case PhilosopherAction.ReleaseRightFork:
                    ReleaseRightFork();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown action");
            }
        }

        private void ProcessCurrentState()
        {
            switch (_state)
            {
                case PhilosopherState.Thinking:
                    int thinkTime = _rng.Next(_thinkMin, _thinkMax + 1);
                    SleepWithCancellation(thinkTime);
                    // finished thinking => hungry
                    _state = PhilosopherState.Hungry;
                    _hungryStartTime = Environment.TickCount64;
                    break;

                case PhilosopherState.Hungry:
                    // check if both forks are acquired
                    if (_hasLeftFork && _hasRightFork)
                    {
                        // save waiting time
                        long waited = Environment.TickCount64 - _hungryStartTime;
                        Interlocked.Add(ref _waitingTotal, waited);

                        _leftFork.MarkEating();
                        _rightFork.MarkEating();

                        // both forks acquired => eating
                        _state = PhilosopherState.Eating;
                    }
                    break;

                case PhilosopherState.Eating:
                    int eatTime = _rng.Next(_eatMin, _eatMax + 1);
                    SleepWithCancellation(eatTime);

                    Interlocked.Increment(ref _eatenCount);

                    ReleaseAllForks();
                    // finished eating => thinking
                    _state = PhilosopherState.Thinking;
                    break;
            }
        }

        private void TryTakeLeftFork()
        {
            if (_hasLeftFork)
            {
                return;
            }

            if (_leftFork.TryTake(Name, ForkAcquireTimeoutMs))
            {
                SleepWithCancellation(_takeForkMs);
                _hasLeftFork = true;
            }
        }

        private void TryTakeRightFork()
        {
            if (_hasRightFork)
            {
                return;
            }

            if (_rightFork.TryTake(Name, ForkAcquireTimeoutMs))
            {
                SleepWithCancellation(_takeForkMs);
                _hasRightFork = true;
            }
        }

        private void ReleaseLeftFork()
        {
            if (!_hasLeftFork)
            {
                return;
            }

            _leftFork.Release(Name);
            _hasLeftFork = false;
        }

        private void ReleaseRightFork()
        {
            if (!_hasRightFork)
            {
                return;
            }

            _rightFork.Release(Name);
            _hasRightFork = false;
        }

        private void ReleaseAllForks()
        {
            ReleaseLeftFork();
            ReleaseRightFork();
        }

        public PhilosopherView AsView()
        {
            return new PhilosopherView
            {
                Id = Id,
                Name = Name,
                State = _state,
                HasLeftFork = _hasLeftFork,
                HasRightFork = _hasRightFork,
                EatenCount = Interlocked.Read(ref _eatenCount),
                WaitingMilliseconds = Interlocked.Read(ref _waitingTotal)
            };
        }
    }
}

using System.Diagnostics;
using DiningPhilosophers.Core.ForkUtils;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Utility;

namespace DiningPhilosophers.Core.Services;

public sealed class TableManager : ITableManager
{
    private sealed class ForkRecord
    {
        public ForkRecord(long timestamp)
        {
            LastTimestamp = timestamp;
        }

        public ForkState State { get; set; } = ForkState.Available;
        public string? Owner { get; set; }
        public bool IsEating { get; set; }
        public long LastTimestamp { get; set; }
        public long AvailableTicks { get; set; }
        public long QueuedTicks { get; set; }
        public long InUseTicks { get; set; }
        public long EatingTicks { get; set; }
    }

    private readonly object _sync = new();
    private readonly ForkRecord[] _forks;
    private readonly long _startTimestamp;

    public TableManager(int forks)
    {
        if (forks <= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(forks), "Table must have at least two forks.");
        }

        _startTimestamp = Stopwatch.GetTimestamp();
        _forks = new ForkRecord[forks];
        for (int i = 0; i < forks; i++)
        {
            _forks[i] = new ForkRecord(_startTimestamp);
        }
    }

    public bool TryAcquireForks(string philosopherName, int leftForkIndex, int rightForkIndex)
    {
        lock (_sync)
        {
            if (!IsForkAvailable(leftForkIndex) || !IsForkAvailable(rightForkIndex))
            {
                return false;
            }

            SetForkState(leftForkIndex, philosopherName, ForkState.InUse, isEating: false);
            SetForkState(rightForkIndex, philosopherName, ForkState.InUse, isEating: false);
            return true;
        }
    }

    public void ReleaseForks(string philosopherName, int leftForkIndex, int rightForkIndex)
    {
        lock (_sync)
        {
            if (_forks[leftForkIndex].Owner == philosopherName)
            {
                SetForkState(leftForkIndex, null, ForkState.Available, isEating: false);
            }

            if (_forks[rightForkIndex].Owner == philosopherName)
            {
                SetForkState(rightForkIndex, null, ForkState.Available, isEating: false);
            }
        }
    }

    public void MarkEating(string philosopherName, int leftForkIndex, int rightForkIndex)
    {
        lock (_sync)
        {
            MarkForkEating(leftForkIndex, philosopherName);
            MarkForkEating(rightForkIndex, philosopherName);
        }
    }

    public IReadOnlyList<ForkStatus> GetStatus()
    {
        lock (_sync)
        {
            var snapshot = new ForkStatus[_forks.Length];
            for (int i = 0; i < _forks.Length; i++)
            {
                var fork = _forks[i];
                snapshot[i] = new ForkStatus(i, fork.State, fork.Owner);
            }
            return snapshot;
        }
    }

    public IReadOnlyList<ForkUtilization> GetUtilization()
    {
        lock (_sync)
        {
            var now = Stopwatch.GetTimestamp();
            for (int i = 0; i < _forks.Length; i++)
            {
                AccumulateDuration(_forks[i], now);
            }

            var totalTicks = Math.Max(1, now - _startTimestamp);
            var snapshot = new ForkUtilization[_forks.Length];
            for (int i = 0; i < _forks.Length; i++)
            {
                var fork = _forks[i];
                snapshot[i] = new ForkUtilization(
                    i + 1,
                    Percent(fork.AvailableTicks, totalTicks),
                    Percent(fork.QueuedTicks, totalTicks),
                    Percent(fork.InUseTicks, totalTicks),
                    Percent(fork.EatingTicks, totalTicks));
            }

            return snapshot;
        }
    }

    private bool IsForkAvailable(int index) => _forks[index].State == ForkState.Available;

    private void SetForkState(int index, string? owner, ForkState state, bool isEating)
    {
        var fork = _forks[index];
        var now = Stopwatch.GetTimestamp();
        AccumulateDuration(fork, now);
        fork.Owner = owner;
        fork.State = state;
        fork.IsEating = isEating;
        fork.LastTimestamp = now;
    }

    private static void AccumulateDuration(ForkRecord fork, long now)
    {
        var delta = now - fork.LastTimestamp;
        if (delta <= 0)
        {
            return;
        }

        switch (fork.State)
        {
            case ForkState.Available:
                fork.AvailableTicks += delta;
                break;
            case ForkState.Queued:
                fork.QueuedTicks += delta;
                break;
            case ForkState.InUse:
                if (fork.IsEating)
                {
                    fork.EatingTicks += delta;
                }
                else
                {
                    fork.InUseTicks += delta;
                }
                break;
        }

        fork.LastTimestamp = now;
    }

    private void MarkForkEating(int index, string philosopherName)
    {
        var fork = _forks[index];
        if (fork.Owner != philosopherName || fork.State != ForkState.InUse || fork.IsEating)
        {
            return;
        }

        var now = Stopwatch.GetTimestamp();
        AccumulateDuration(fork, now);
        fork.IsEating = true;
        fork.LastTimestamp = now;
    }

    private static double Percent(long ticks, long totalTicks)
    {
        if (totalTicks <= 0)
        {
            return 0d;
        }

        return ticks * 100.0 / totalTicks;
    }
}

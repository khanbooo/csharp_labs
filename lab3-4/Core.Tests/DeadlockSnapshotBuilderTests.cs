using System.Linq;
using DiningPhilosophers.Core.ForkUtils;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Utility;

namespace Core.Tests;

public class DeadlockSnapshotBuilderTests
{
    [Fact]
    public void BuildSnapshots_MapsSeatStateAndForkOwnership()
    {
        var seats = new[]
        {
            new PhilosopherSeat("Платон", 0, 3),
            new PhilosopherSeat("Аристотель", 1, 3),
            new PhilosopherSeat("Сократ", 2, 3)
        };

        var report = new SimulationReport
        {
            Philosophers = new[]
            {
                new PhilosopherSummary { Name = "Платон", CurrentState = PhilosopherRuntimeState.Hungry },
                new PhilosopherSummary { Name = "Аристотель", CurrentState = PhilosopherRuntimeState.Eating }
            },
            Forks = Array.Empty<ForkStatus>(),
            ForkUtilization = Array.Empty<ForkUtilization>(),
            TotalThinkingMs = 0,
            TotalEatingMs = 0,
            SimulationDurationMs = 0
        };

        var forkStatuses = new[]
        {
            new ForkStatus(0, ForkState.InUse, "Платон"),
            new ForkStatus(1, ForkState.InUse, "Платон"),
            new ForkStatus(2, ForkState.InUse, "Аристотель")
        };

        var snapshots = DeadlockSnapshotBuilder.BuildSnapshots(seats, report, forkStatuses);

        Assert.Equal(3, snapshots.Count);

        var plato = Assert.Single(snapshots.Where(s => s.Name == "Платон"));
        Assert.True(plato.IsHungry);
        Assert.True(plato.HasLeftFork);
        Assert.True(plato.HasRightFork);

        var aristotle = Assert.Single(snapshots.Where(s => s.Name == "Аристотель"));
        Assert.False(aristotle.IsHungry);
        Assert.False(aristotle.HasLeftFork);
        Assert.True(aristotle.HasRightFork);

        var socrates = Assert.Single(snapshots.Where(s => s.Name == "Сократ"));
        Assert.False(socrates.IsHungry);
        Assert.False(socrates.HasLeftFork);
        Assert.False(socrates.HasRightFork);
    }

    [Fact]
    public void BuildDeadlockMessage_ProducesReadableSummary()
    {
        var snapshots = new[]
        {
            new PhilosopherSnapshot("Платон", true, true, false),
            new PhilosopherSnapshot("Сократ", true, false, true)
        };

        var message = DeadlockSnapshotBuilder.BuildDeadlockMessage(snapshots);

        Assert.Contains("DEADLOCK DETECTED", message);
        Assert.Contains("Платон: HasLeftFork=YES, HasRightFork=no", message);
        Assert.Contains("Сократ: HasLeftFork=no, HasRightFork=YES", message);
    }

    [Fact]
    public void BuildSnapshots_AllHungryHoldingOneFork_DetectedByDeadlockDetector()
    {
        var seats = new[]
        {
            new PhilosopherSeat("Платон", 0, 3),
            new PhilosopherSeat("Аристотель", 1, 3),
            new PhilosopherSeat("Сократ", 2, 3)
        };

        var report = new SimulationReport
        {
            Philosophers = new[]
            {
                new PhilosopherSummary { Name = "Платон", CurrentState = PhilosopherRuntimeState.Hungry },
                new PhilosopherSummary { Name = "Аристотель", CurrentState = PhilosopherRuntimeState.Hungry },
                new PhilosopherSummary { Name = "Сократ", CurrentState = PhilosopherRuntimeState.Hungry }
            },
            Forks = Array.Empty<ForkStatus>(),
            ForkUtilization = Array.Empty<ForkUtilization>(),
            TotalThinkingMs = 0,
            TotalEatingMs = 0,
            SimulationDurationMs = 0
        };

        var forkStatuses = new[]
        {
            new ForkStatus(0, ForkState.InUse, "Платон"),
            new ForkStatus(1, ForkState.InUse, "Аристотель"),
            new ForkStatus(2, ForkState.InUse, "Сократ")
        };

        var snapshots = DeadlockSnapshotBuilder.BuildSnapshots(seats, report, forkStatuses);

        Assert.True(DeadlockDetector.IsDeadlocked(snapshots));
    }
}

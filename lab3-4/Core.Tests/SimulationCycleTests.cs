using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Utility;
using Xunit;

namespace Core.Tests;

public class SimulationCycleTests
{
    [Fact]
    public void PhilosopherCycle_AvailableForks_CompletesThinkHungryEatSequence()
    {
        var table = new TableManager(forks: 5);
        var metrics = new MetricsCollector();
        var seat = new PhilosopherSeat("Сократ", index: 0, totalPhilosophers: 5);
        metrics.RegisterPhilosopher(seat.Name);

        metrics.RecordThinking(seat.Name, milliseconds: 120);

        var acquired = table.TryAcquireForks(seat.Name, seat.LeftForkIndex, seat.RightForkIndex);
        Assert.True(acquired);

        table.MarkEating(seat.Name, seat.LeftForkIndex, seat.RightForkIndex);
        metrics.RecordWaiting(seat.Name, milliseconds: 15);
        metrics.RecordEating(seat.Name, milliseconds: 60);
        table.ReleaseForks(seat.Name, seat.LeftForkIndex, seat.RightForkIndex);

        var report = metrics.BuildReport(table.GetStatus());
        var philosopher = Assert.Single(report.Philosophers);

        Assert.Equal(120, philosopher.TotalThinkingMs);
        Assert.Equal(60, philosopher.TotalEatingMs);
        Assert.Equal(1, philosopher.EatingCount);
        Assert.Equal(15, philosopher.TotalWaitingMs);
    }

    [Fact]
    public void PhilosopherCycle_BlockedOnBusyForks_RemainsHungryUntilForkReleased()
    {
        var table = new TableManager(forks: 5);
        var seatOne = new PhilosopherSeat("Платон", 0, 5);
        var seatTwo = new PhilosopherSeat("Аристотель", 1, 5);

        var firstAcquired = table.TryAcquireForks(seatOne.Name, seatOne.LeftForkIndex, seatOne.RightForkIndex);
        Assert.True(firstAcquired);

        var secondAcquired = table.TryAcquireForks(seatTwo.Name, seatTwo.LeftForkIndex, seatTwo.RightForkIndex);
        Assert.False(secondAcquired);

        table.ReleaseForks(seatOne.Name, seatOne.LeftForkIndex, seatOne.RightForkIndex);

        var secondRetry = table.TryAcquireForks(seatTwo.Name, seatTwo.LeftForkIndex, seatTwo.RightForkIndex);
        Assert.True(secondRetry);
    }
}

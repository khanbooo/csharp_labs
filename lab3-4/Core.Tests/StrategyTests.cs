using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Strategies;
using Moq;

namespace Core.Tests;

public class StrategyTests
{
    [Fact]
    public void LeftRightStrategy_TryAcquireForks_UsesSeatOrder()
    {
        var seat = new PhilosopherSeat("Платон", index: 2, totalPhilosophers: 5);
        var tableManager = new Mock<ITableManager>(MockBehavior.Strict);
        tableManager
            .Setup(tm => tm.TryAcquireForks(seat.Name, seat.LeftForkIndex, seat.RightForkIndex))
            .Returns(true)
            .Verifiable();

        var strategy = new LeftRightStrategy();

        var result = strategy.TryAcquireForks(seat, tableManager.Object);

        Assert.True(result);
        tableManager.Verify();
    }

    [Fact]
    public void ResourceHierarchyStrategy_TryAcquireForks_UsesLowerIndexedForkFirst()
    {
        var seat = new PhilosopherSeat("Сократ", index: 4, totalPhilosophers: 5); // left=4, right=0
        var tableManager = new Mock<ITableManager>(MockBehavior.Strict);
        tableManager
            .Setup(tm => tm.TryAcquireForks(seat.Name, 0, 4))
            .Returns(true)
            .Verifiable();

        var strategy = new ResourceHierarchyStrategy();

        var result = strategy.TryAcquireForks(seat, tableManager.Object);

        Assert.True(result);
        tableManager.Verify();
    }
}

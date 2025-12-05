using DiningPhilosophers.Core.Utility;

namespace Core.Tests;

public class DeadlockDetectorTests
{
    [Fact]
    public void IsDeadlocked_AllHungryHoldingSingleFork_ReturnsTrue()
    {
        var snapshots = new[]
        {
            new PhilosopherSnapshot("Платон", IsHungry: true, HasLeftFork: true, HasRightFork: false),
            new PhilosopherSnapshot("Аристотель", IsHungry: true, HasLeftFork: false, HasRightFork: true),
            new PhilosopherSnapshot("Сократ", IsHungry: true, HasLeftFork: true, HasRightFork: false)
        };

        var result = DeadlockDetector.IsDeadlocked(snapshots);

        Assert.True(result);
    }

    [Fact]
    public void IsDeadlocked_PhilosoherEating_ReturnsFalse()
    {
        var snapshots = new[]
        {
            new PhilosopherSnapshot("Платон", IsHungry: true, HasLeftFork: true, HasRightFork: false),
            new PhilosopherSnapshot("Аристотель", IsHungry: false, HasLeftFork: true, HasRightFork: true)
        };

        var result = DeadlockDetector.IsDeadlocked(snapshots);

        Assert.False(result);
    }

    [Fact]
    public void IsDeadlocked_IdlePhilosopherPresent_ReturnsFalse()
    {
        var snapshots = new[]
        {
            new PhilosopherSnapshot("Платон", IsHungry: true, HasLeftFork: true, HasRightFork: false),
            new PhilosopherSnapshot("Аристотель", IsHungry: true, HasLeftFork: false, HasRightFork: true),
            new PhilosopherSnapshot("Сократ", IsHungry: true, HasLeftFork: false, HasRightFork: false)
        };

        var result = DeadlockDetector.IsDeadlocked(snapshots);

        Assert.False(result);
    }
}

using System.Diagnostics;
using System.Reflection;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Utility;

namespace Core.Tests;

public class TableManagerUtilizationTests
{
    [Fact]
    public void GetUtilization_NormalizesDurationsIntoPercentages()
    {
        var manager = new TableManager(2);
        var desiredTotalTicks = 1_000_000L;
        var now = Stopwatch.GetTimestamp();

        SetPrivateField(manager, "_startTimestamp", now - desiredTotalTicks);

        var forksField = typeof(TableManager).GetField("_forks", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var forks = (Array)forksField.GetValue(manager)!;
        var forkType = forks.GetType().GetElementType()!;

        ConfigureFork(forks, forkType, 0, 200_000, 100_000, 300_000, 400_000);
        ConfigureFork(forks, forkType, 1, 150_000, 250_000, 350_000, 250_000);

        var utilization = manager.GetUtilization();

        Assert.Equal(2, utilization.Count);

        var first = utilization[0];
        Assert.Equal(1, first.ForkId);
        AssertRatio(first.AvailablePercent, first.QueuedPercent, 200_000d, 100_000d);
        AssertRatio(first.InUsePercent, first.EatingPercent, 300_000d, 400_000d);

        var second = utilization[1];
        Assert.Equal(2, second.ForkId);
        AssertRatio(second.AvailablePercent, second.QueuedPercent, 150_000d, 250_000d);
        AssertRatio(second.InUsePercent, second.EatingPercent, 350_000d, 250_000d);
    }

    private static void ConfigureFork(Array forks, Type forkType, int index, long available, long queued, long inUse, long eating)
    {
        var fork = forks.GetValue(index)!;
        SetMember(forkType, fork, "AvailableTicks", available);
        SetMember(forkType, fork, "QueuedTicks", queued);
        SetMember(forkType, fork, "InUseTicks", inUse);
        SetMember(forkType, fork, "EatingTicks", eating);
        SetMember(forkType, fork, "LastTimestamp", long.MaxValue);
    }

    private static void SetPrivateField(object target, string name, long value)
    {
        var field = typeof(TableManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(target, value);
    }

    private static void SetMember(Type type, object target, string name, long value)
    {
        var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (property != null)
        {
            property.SetValue(target, value);
            return;
        }

        var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
        field!.SetValue(target, value);
    }

    private static void AssertRatio(double percentA, double percentB, double ticksA, double ticksB)
    {
        var expectedRatio = ticksA / ticksB;
        var actualRatio = percentA / percentB;
        Assert.Equal(expectedRatio, actualRatio, 3);
    }
}

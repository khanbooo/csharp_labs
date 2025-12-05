using System.Linq;
using DiningPhilosophers.Core.ForkUtils;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Utility;

namespace Core.Tests;

public class MetricsCollectorTests
{
    [Fact]
    public void RecordWaiting_NonPositiveDuration_Ignored()
    {
        var collector = new MetricsCollector();
        collector.RegisterPhilosopher("Кант");

        collector.RecordWaiting("Кант", 0);
        collector.RecordWaiting("Кант", -10);

        var report = collector.BuildReport(Array.Empty<ForkStatus>());
        var summary = Assert.Single(report.Philosophers);

        Assert.Equal(0, summary.TotalWaitingMs);
    }

    [Fact]
    public void BuildReport_AggregatesTotalsForAllPhilosophers()
    {
        var collector = new MetricsCollector();
        collector.RegisterPhilosopher("Платон");
        collector.RegisterPhilosopher("Сократ");

        collector.RecordThinking("Платон", 100);
        collector.RecordEating("Платон", 50);
        collector.RecordWaiting("Платон", 20);
        collector.RecordFailedAttempt("Платон");

        collector.RecordThinking("Сократ", 150);
        collector.RecordEating("Сократ", 60);
        collector.RecordWaiting("Сократ", 10);

        var forkStatuses = new[] { new ForkStatus(0, ForkState.Available, null) };
        var report = collector.BuildReport(forkStatuses);

        Assert.Equal(250, report.TotalThinkingMs);
        Assert.Equal(110, report.TotalEatingMs);

        var plato = Assert.Single(report.Philosophers.Where(p => p.Name == "Платон"));
        Assert.Equal(20, plato.TotalWaitingMs);
        Assert.Equal(1, plato.EatingCount);
        Assert.Equal(1, plato.FailedAttempts);

        var socrates = Assert.Single(report.Philosophers.Where(p => p.Name == "Сократ"));
        Assert.Equal(10, socrates.TotalWaitingMs);
        Assert.Equal(1, socrates.EatingCount);
    }
}

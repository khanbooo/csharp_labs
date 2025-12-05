using DiningPhilosophers.Core.ForkUtils;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Utility;

namespace Core.Tests;

public class MetricsCollectorStateTests
{
    [Fact]
    public void SetState_UpdatesCurrentState()
    {
        var collector = new MetricsCollector();
        collector.RegisterPhilosopher("Платон");

        collector.SetState("Платон", PhilosopherRuntimeState.Hungry);
        collector.SetState("Платон", PhilosopherRuntimeState.Eating);

        var report = collector.BuildReport(Array.Empty<ForkStatus>());
        var summary = Assert.Single(report.Philosophers);
        Assert.Equal(PhilosopherRuntimeState.Eating, summary.CurrentState);
    }

    [Fact]
    public void BuildReport_DefaultStateIsThinking()
    {
        var collector = new MetricsCollector();
        collector.RegisterPhilosopher("Сократ");

        var report = collector.BuildReport(Array.Empty<ForkStatus>());
        var summary = Assert.Single(report.Philosophers);
        Assert.Equal(PhilosopherRuntimeState.Thinking, summary.CurrentState);
    }
}

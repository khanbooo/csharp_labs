using DiningPhilosophers.Core.Strategies;

namespace Core.Tests;

public class StrategySelectorTests
{
    [Theory]
    [InlineData("Hierarchy")]
    [InlineData("hierarchy")]
    [InlineData("  Hierarchy  ")]
    public void Create_HierarchyKeyword_ReturnsResourceStrategy(string? value)
    {
        var strategy = StrategySelector.Create(value);

        Assert.IsType<ResourceHierarchyStrategy>(strategy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("LeftRight")]
    [InlineData("unknown")]
    public void Create_OtherValues_ReturnsLeftRightStrategy(string? value)
    {
        var strategy = StrategySelector.Create(value);

        Assert.IsType<LeftRightStrategy>(strategy);
    }
}

using System;
using DiningPhilosophers.Core.Interfaces;

namespace DiningPhilosophers.Core.Strategies;

public static class StrategySelector
{
    public static IPhilosopherStrategy Create(string? strategyName)
    {
        if (string.Equals(strategyName?.Trim(), "Hierarchy", StringComparison.OrdinalIgnoreCase))
        {
            return new ResourceHierarchyStrategy();
        }

        return new LeftRightStrategy();
    }
}

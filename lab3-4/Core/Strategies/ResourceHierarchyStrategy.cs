using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Services;

namespace DiningPhilosophers.Core.Strategies;

public sealed class ResourceHierarchyStrategy : IPhilosopherStrategy
{
    public bool TryAcquireForks(PhilosopherSeat seat, ITableManager tableManager)
    {
        if (tableManager == null)
        {
            throw new ArgumentNullException(nameof(tableManager));
        }

        if (seat == null)
        {
            throw new ArgumentNullException(nameof(seat));
        }

        var first = Math.Min(seat.LeftForkIndex, seat.RightForkIndex);
        var second = Math.Max(seat.LeftForkIndex, seat.RightForkIndex);
        return tableManager.TryAcquireForks(seat.Name, first, second);
    }
}

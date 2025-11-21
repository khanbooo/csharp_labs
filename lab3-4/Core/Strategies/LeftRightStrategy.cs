using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Services;

namespace DiningPhilosophers.Core.Strategies;

public sealed class LeftRightStrategy : IPhilosopherStrategy
{
    public bool TryAcquireForks(PhilosopherSeat seat, ITableManager tableManager)
    {
        // Simple strategy: always pick left fork first, then right fork.
        return tableManager.TryAcquireForks(seat.Name, seat.LeftForkIndex, seat.RightForkIndex);
    }
}

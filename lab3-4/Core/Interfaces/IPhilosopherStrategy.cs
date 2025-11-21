using DiningPhilosophers.Core.Services;

namespace DiningPhilosophers.Core.Interfaces;

public interface IPhilosopherStrategy
{
    bool TryAcquireForks(PhilosopherSeat seat, ITableManager tableManager);
}

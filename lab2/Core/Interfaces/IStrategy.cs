using DiningPhilosophers.Core.Entities.Multithreaded;
using DiningPhilosophers.Core.PhilosopherUtils;

namespace DiningPhilosophers.Core.Interfaces
{
    public interface IStrategy
    {
        PhilosopherAction Decide(PhilosopherView view);
    }
}


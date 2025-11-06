using DiningPhilosophers.Core.Entities.Multithreaded;
using DiningPhilosophers.Core.PhilosopherUtils;

namespace DiningPhilosophers.Core.Utility
{
    public static class DeadlockDetector
    {
        public static bool IsDeadlocked(List<Philosopher> philosophers)
        {
            if (philosophers == null || philosophers.Count == 0)
            {
                return false;
            }

            // Check if all philosophers are hungry
            bool allHungry = philosophers.All(p =>
            {
                var view = p.AsView();
                return view.State == PhilosopherState.Hungry;
            });

            if (!allHungry)
            {
                return false;
            }

            // Check that each philosopher holds exactly one fork
            int philosophersWithOneFork = 0;
            foreach (var philosopher in philosophers)
            {
                var view = philosopher.AsView();
                int forksHeld = (view.HasLeftFork ? 1 : 0) + (view.HasRightFork ? 1 : 0);

                if (forksHeld == 1)
                {
                    philosophersWithOneFork++;
                }
                else if (forksHeld == 0 || forksHeld == 2)
                {
                    // If any philosopher holds 0 or 2 forks, it's not a deadlock
                    return false;
                }
            }

            // Deadlock: all philosophers are hungry and each holds exactly one fork
            return philosophersWithOneFork == philosophers.Count;
        }
    }
}

using System.Collections.Generic;
using System.Linq;

namespace DiningPhilosophers.Core.Utility;

public static class DeadlockDetector
{
    public static bool IsDeadlocked(IEnumerable<PhilosopherSnapshot> snapshots)
    {
        if (snapshots == null)
        {
            return false;
        }

        var list = snapshots.ToList();
        if (list.Count == 0)
        {
            return false;
        }

        if (list.Any(s => !s.IsHungry))
        {
            return false;
        }

        if (list.Any(s => s.HasLeftFork == s.HasRightFork))
        {
            // Either both forks or none -> no deadlock.
            return false;
        }

        // All philosophers are hungry and each holds exactly one fork -> deadlock.
        return true;
    }
}

public sealed record PhilosopherSnapshot(string Name, bool IsHungry, bool HasLeftFork, bool HasRightFork);

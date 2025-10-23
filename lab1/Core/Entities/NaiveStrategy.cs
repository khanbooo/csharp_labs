using System.Text;

namespace DiningPhilosophers.Core
{
    namespace Strategy
    {
        public class NaiveStrategy : IStrategy
        {
            public PhilosopherUtils.PhilosopherAction Decide(Philosopher.PhilosopherView philosopher, string output, long Step)
            {
                if (philosopher.IsBusy)
                {
                    return PhilosopherUtils.PhilosopherAction.None;
                }
                if (philosopher.LeftFork.State == ForkUtils.ForkState.Available &&
                    philosopher.RightFork.State == ForkUtils.ForkState.Available)
                {
                    return PhilosopherUtils.PhilosopherAction.TakeLeftFork;
                }
                if (philosopher.LeftFork.Owner == philosopher.Name &&
                    philosopher.RightFork.State == ForkUtils.ForkState.Available)
                {
                    return PhilosopherUtils.PhilosopherAction.TakeRightFork;
                }
                return PhilosopherUtils.PhilosopherAction.None;
            }
        }

    }
}
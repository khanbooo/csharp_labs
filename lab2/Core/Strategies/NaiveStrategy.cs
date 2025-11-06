using DiningPhilosophers.Core.Entities.Multithreaded;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.PhilosopherUtils;

namespace DiningPhilosophers.Core.Strategies
{
    public class NaiveStrategy : IStrategy
    {
        public PhilosopherAction Decide(PhilosopherView view)
        {
            switch (view.State)
            {
                case PhilosopherState.Thinking:
                    return PhilosopherAction.None;
                case PhilosopherState.Hungry:
                    if (!view.HasLeftFork && view.LeftFork?.Owner == null)
                    {
                        return PhilosopherAction.TakeLeftFork;
                    }
                    if (view.HasLeftFork && !view.HasRightFork && view.RightFork?.Owner == null)
                    {
                        return PhilosopherAction.TakeRightFork;
                    }
                    return PhilosopherAction.None;
                case PhilosopherState.Eating:
                    return PhilosopherAction.None;
                default:
                    return PhilosopherAction.None;
            }
        }
    }
}
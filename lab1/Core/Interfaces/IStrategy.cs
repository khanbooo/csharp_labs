namespace DiningPhilosophers.Core
{
    namespace Strategy
    {
        public interface IStrategy
        {
            public PhilosopherUtils.PhilosopherAction Decide(Philosopher.PhilosopherView philosopher, string output, long Step);
        }
    }
}


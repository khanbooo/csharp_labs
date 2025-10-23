namespace DiningPhilosophers.Core
{
    namespace PhilosopherUtils
    {

        public enum PhilosopherState { Thinking, Hungry, Eating }
        public enum PhilosopherAction
        {
            TakeRightFork,
            TakeLeftFork,
            ReleaseRightFork,
            ReleaseLeftFork,
            None
        }
    }
}
using DiningPhilosophers.Core.Entities;
using DiningPhilosophers.Core.Interfaces;
using PhilosopherImpl = DiningPhilosophers.Core.Entities.Multithreaded.Philosopher;

namespace DiningPhilosophers.Core.Factories
{
    public sealed class PhilosopherFactory
    {
        private readonly IStrategy _strategy;
        private readonly int _thinkMin;
        private readonly int _thinkMax;
        private readonly int _eatMin;
        private readonly int _eatMax;
        private readonly int _takeForkMs;

        public PhilosopherFactory(
            IStrategy strategy,
            int thinkMin,
            int thinkMax,
            int eatMin,
            int eatMax,
            int takeForkMs)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            _thinkMin = thinkMin;
            _thinkMax = thinkMax;
            _eatMin = eatMin;
            _eatMax = eatMax;
            _takeForkMs = takeForkMs;
        }

        public IPhilosopher Create(
            int id,
            string name,
            Fork leftFork,
            Fork rightFork,
            CancellationToken cancellation,
            long startTime)
        {
            return new PhilosopherImpl(
                id,
                name,
                leftFork,
                rightFork,
                _strategy,
                cancellation,
                startTime,
                _thinkMin,
                _thinkMax,
                _eatMin,
                _eatMax,
                _takeForkMs
            );
        }
    }
}

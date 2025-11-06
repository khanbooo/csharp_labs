using DiningPhilosophers.Core.Entities;

namespace DiningPhilosophers.Core.Factories
{
    public sealed class ForkFactory
    {
        public Fork Create(int id)
        {
            return new Fork(id);
        }

        public Fork[] CreateMany(int count)
        {
            var forks = new Fork[count];
            for (int i = 0; i < count; i++)
            {
                forks[i] = Create(i);
            }
            return forks;
        }
    }
}

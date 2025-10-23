namespace DiningPhilosophers.Core
{
    namespace Coordinator
    {
        public interface ICoordinator
        {
            event Action<List<Fork.Fork>>? OnForksProvided;
            event Action<int, PhilosopherUtils.PhilosopherAction>? OnAction;

            void ProvideForks(List<Fork.Fork> forks);
            void DeclareHungry(int philosopherId);
        }
    }
}
namespace DiningPhilosophers.Core
{
    namespace Coordinator
    {
        public class SimpleCoordinator : ICoordinator
        {
            private int _n;
            private List<Fork.Fork>? _forks;

            public event Action<List<Fork.Fork>>? OnForksProvided;
            public event Action<int, PhilosopherUtils.PhilosopherAction>? OnAction;

            public List<Fork.Fork> GetForks()
            {
                return _forks ?? throw new InvalidOperationException("Forks not set yet");
            }

            public void SetForks(List<Fork.Fork> forks)
            {
                if (_forks != null)
                    throw new InvalidOperationException("Forks already set");
                _forks = forks ?? throw new ArgumentNullException(nameof(forks));
                _n = forks.Count;
            }

            public void ProvideForks(List<Fork.Fork> forks)
            {
                OnForksProvided?.Invoke(forks);
            }

            public SimpleCoordinator()
            {
                OnForksProvided += SetForks;
            }

            public void DeclareHungry(int philosopherId)
            {
                int leftId = philosopherId;
                int rightId = (philosopherId + 1) % _n;
                var left = _forks![leftId];
                var right = _forks![rightId];

                if (left.State == ForkUtils.ForkState.Available && right.State == ForkUtils.ForkState.Available)
                {
                    OnAction?.Invoke(philosopherId, PhilosopherUtils.PhilosopherAction.TakeLeftFork);
                    OnAction?.Invoke(philosopherId, PhilosopherUtils.PhilosopherAction.TakeRightFork);
                }
            }
        }
    }
}
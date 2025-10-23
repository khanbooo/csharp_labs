
namespace DiningPhilosophers.Core
{
    namespace Philosopher
    {
        public class PhilosopherView
        {
            public int Id { get; init; }
            public string Name { get; init; } = string.Empty;
            public PhilosopherUtils.PhilosopherState State { get; init; }
            public PhilosopherUtils.PhilosopherState Action { get; init; }
            public bool IsBusy { get; init; } = false;
            public bool HasLeftFork { get; init; }
            public bool HasRightFork { get; init; }
            public int TakingForkTimeLeft { get; init; }
            public int EatingTimeLeft { get; init; }
            public int ThinkingTimeLeft { get; init; }
            public int EatenCount { get; init; }
            public Fork.ForkView LeftFork { get; init; } = null!;
            public Fork.ForkView RightFork { get; init; } = null!;
        }

        public class Philosopher
        {
            private readonly int FORK_TAKING_TIME = 1;

            public int Id { get; private set; }
            public string Name { get; private set; }
            public PhilosopherUtils.PhilosopherState State { get; set; }
            public PhilosopherUtils.PhilosopherState Action { get; set; }

            public bool IsBusy { get; private set; } = false;

            public bool HasLeftFork { get; private set; } = false;
            public bool HasRightFork { get; private set; } = false;

            public int TakingForkTimeLeft { get; set; } = 0;
            public int EatingTimeLeft { get; set; } = 0;
            public int ThinkingTimeLeft { get; set; } = 0;

            public int EatenCount { get; private set; } = 0;

            public void StartTakingFork(Fork.Fork fork)
            {
                IsBusy = true;
                TakingForkTimeLeft = FORK_TAKING_TIME;
                fork.Use(Name);
            }

            public void FinishTakingFork(Fork.Fork fork)
            {
                IsBusy = false;
                if (Id == fork.Id)
                {
                    HasLeftFork = true;
                }
                else
                {
                    HasRightFork = true;
                }
            }

            public void ReleaseFork(Fork.Fork fork)
            {
                HasLeftFork = false;
                fork.Release();
            }

            public void StartEating(int EatingTime)
            {
                IsBusy = true;
                EatingTimeLeft = EatingTime;
                State = PhilosopherUtils.PhilosopherState.Eating;
            }

            public void EndEating()
            {
                IsBusy = false;
                EatenCount++;
            }

            public void StartThinking(int ThinkingTime)
            {
                IsBusy = true;
                ThinkingTimeLeft = ThinkingTime;
                State = PhilosopherUtils.PhilosopherState.Thinking;
            }

            public void EndThinking()
            {
                IsBusy = false;
                State = PhilosopherUtils.PhilosopherState.Hungry;
            }

            public Philosopher(int id, string name, int startupThinkingTime)
            {
                Id = id;
                Name = name;
                StartThinking(startupThinkingTime);
            }

            public PhilosopherView AsView(Fork.Fork leftFork, Fork.Fork rightFork) => new()
            {
                Id = Id,
                Name = Name,
                State = State,
                Action = Action,
                IsBusy = IsBusy,
                HasLeftFork = HasLeftFork,
                HasRightFork = HasRightFork,
                TakingForkTimeLeft = TakingForkTimeLeft,
                EatingTimeLeft = EatingTimeLeft,
                ThinkingTimeLeft = ThinkingTimeLeft,
                EatenCount = EatenCount,
                LeftFork = leftFork.AsView(),
                RightFork = rightFork.AsView()
            };
        }
    }
}
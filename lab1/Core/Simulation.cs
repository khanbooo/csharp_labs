using System.Net.Http.Headers;

namespace DiningPhilosophers.Core
{
    namespace Simulation
    {
        public class Simulation
        {
            private readonly List<Philosopher.Philosopher> _philos;
            private readonly List<Fork.Fork> _forks;
            private readonly Queue<PhilosopherUtils.PhilosopherAction>[] _actionQueues;
            private readonly Strategy.IStrategy? _strategy;
            private readonly Coordinator.ICoordinator? _coordinator;
            private readonly Random _rng = new();
            private readonly string _output;

            public long Step { get; private set; } = 0;

            // Metrics
            private readonly long[] _hungryTotalSteps; // total steps spent hungry per philosopher
            private readonly long[] _forkAvailableSteps; // per-fork counters
            private readonly long[] _forkQueuedSteps;
            private readonly long[] _forkInUseSteps; // in-use while being taken (not eating)
            private readonly long[] _forkInEatingSteps; // in-use while actually eating


            public Simulation(string[] names, Strategy.IStrategy? strategy, Coordinator.ICoordinator? coordinator, string output)
            {
                if ((strategy == null && coordinator == null) || (strategy != null && coordinator != null))
                    throw new ArgumentException("Provide exactly one of strategy or coordinator");
                int n = names.Length;
                _philos = new List<Philosopher.Philosopher>(n);
                _forks = new List<Fork.Fork>(n);
                // metrics arrays
                _hungryTotalSteps = new long[n];
                _forkAvailableSteps = new long[n];
                _forkQueuedSteps = new long[n];
                _forkInUseSteps = new long[n];
                _forkInEatingSteps = new long[n];
                if (coordinator != null)
                {
                    _actionQueues = new Queue<PhilosopherUtils.PhilosopherAction>[n];
                    for (int i = 0; i < n; i++) _actionQueues[i] = new Queue<PhilosopherUtils.PhilosopherAction>();
                }
                else
                {
                    _actionQueues = [];
                }
                for (int i = 0; i < n; i++) _philos.Add(new Philosopher.Philosopher(i, names[i], _rng.Next(3, 11)));
                for (int i = 0; i < n; i++) _forks.Add(new Fork.Fork(i));
                _strategy = strategy;
                _coordinator = coordinator;
                if (coordinator != null)
                {
                    coordinator.ProvideForks(_forks);
                    coordinator.OnAction += HandleCoordinatorAction;
                }
                _output = output;
            }

            private void HandleCoordinatorAction(int id, PhilosopherUtils.PhilosopherAction act)
            {
                if (id < 0 || id >= _actionQueues.Length) return;
                _actionQueues[id].Enqueue(act);
                // mark the fork as queued if action is to take a fork
                if (act == PhilosopherUtils.PhilosopherAction.TakeLeftFork)
                {
                    var left = _forks[id];
                    left.State = ForkUtils.ForkState.Queued;
                }
                else if (act == PhilosopherUtils.PhilosopherAction.TakeRightFork)
                {
                    var right = _forks[(id + 1) % _forks.Count];
                    right.State = ForkUtils.ForkState.Queued;
                }
            }

            public void RunSteps(long steps, int printEvery = 150)
            {
                for (long s = 0; s < steps; s++)
                {
                    Step++;
                    PhilosopherUtils.PhilosopherAction[] actions = GetPhilosophersNextAction();
                    bool[] isActionPossible = _coordinator != null ? [.. Enumerable.Repeat(true, _philos.Count)] : ValidateActions(actions);

                    if (_coordinator != null)
                    {
                        for (int i = 0; i < _philos.Count; i++)
                        {
                            var p = _philos[i];
                            if (p.State == PhilosopherUtils.PhilosopherState.Hungry && _actionQueues[i].Count == 0 && !p.IsBusy)
                            {
                                _coordinator.DeclareHungry(i);
                            }
                        }
                    }

                    if (DetectDeadlock(actions, isActionPossible))
                    {
                        File.AppendAllText(_output, $"Deadlock detected at step {Step}");
                        break;
                    }

                    ApplyActions(actions, isActionPossible);

                    for (int i = 0; i < _philos.Count; i++)
                    {
                        var p = _philos[i];
                        if (p.State == PhilosopherUtils.PhilosopherState.Hungry)
                        {
                            _hungryTotalSteps[i]++;
                        }
                    }

                    for (int i = 0; i < _forks.Count; i++)
                    {
                        var f = _forks[i];
                        switch (f.State)
                        {
                            case ForkUtils.ForkState.Available:
                                _forkAvailableSteps[i]++;
                                break;
                            case ForkUtils.ForkState.Queued:
                                _forkQueuedSteps[i]++;
                                break;
                            case ForkUtils.ForkState.InUse:
                                if (f.Owner != null)
                                {
                                    var owner = _philos.FirstOrDefault(ph => ph.Name == f.Owner);
                                    if (owner != null && owner.State == PhilosopherUtils.PhilosopherState.Eating)
                                    {
                                        _forkInEatingSteps[i]++;
                                    }
                                    _forkInUseSteps[i]++;
                                }
                                else
                                {
                                    _forkInUseSteps[i]++;
                                }
                                break;
                        }
                    }

                    if (Step % printEvery == 0)
                    {
                        File.AppendAllText(_output, GetStatusBlock());
                    }
                }
                File.AppendAllText(_output, GetSummary());
                return;
            }

            private PhilosopherUtils.PhilosopherAction[] GetPhilosophersNextAction()
            {
                PhilosopherUtils.PhilosopherAction[] actions = new PhilosopherUtils.PhilosopherAction[_philos.Count];
                for (int i = 0; i < _philos.Count; i++)
                {
                    if (_coordinator != null)
                    {
                        if (_actionQueues != null && _actionQueues[i].Count > 0 && !_philos[i].IsBusy)
                        {
                            actions[i] = _actionQueues[i].Dequeue();
                        }
                        else
                        {
                            actions[i] = PhilosopherUtils.PhilosopherAction.None;
                        }
                    }
                    else
                    {
                        var p = _philos[i];
                        var left = _forks[i];
                        var right = _forks[(i + 1) % _forks.Count];
                        actions[i] = _strategy!.Decide(p.AsView(left, right), _output, Step);
                    }
                }
                return actions;
            }

            private bool[] ValidateActions(PhilosopherUtils.PhilosopherAction[] actions)
            {
                bool[] isActionPossible = new bool[_philos.Count];
                for (int i = 0; i < _philos.Count; i++)
                {
                    switch (actions[i])
                    {
                        case PhilosopherUtils.PhilosopherAction.TakeLeftFork:
                            int leftPhilosopherIndex = (i - 1 + _philos.Count) % _philos.Count;
                            if (actions[leftPhilosopherIndex] == PhilosopherUtils.PhilosopherAction.TakeRightFork)
                            {
                                isActionPossible[i] = false;
                            }
                            else
                            {
                                isActionPossible[i] = true;
                            }
                            break;
                        case PhilosopherUtils.PhilosopherAction.TakeRightFork:
                            int rightPhilosopherIndex = (i - 1 + _philos.Count) % _philos.Count;
                            if (actions[rightPhilosopherIndex] == PhilosopherUtils.PhilosopherAction.TakeLeftFork)
                            {
                                isActionPossible[i] = false;
                            }
                            else
                            {
                                isActionPossible[i] = true;
                            }
                            break;
                        default:
                            isActionPossible[i] = true;
                            break;
                    }
                }
                return isActionPossible;
            }

            private void ApplyActions(PhilosopherUtils.PhilosopherAction[] actions, bool[] isActionPossible)
            {
                // actions.Count == isActionPossible.Count == _philos.Count
                for (int i = 0; i < _philos.Count; i++)
                {
                    if (isActionPossible[i])
                    {
                        var p = _philos[i];
                        var left = _forks[i];
                        var right = _forks[(i + 1) % _forks.Count];
                        var action = actions[i];
                        switch (action)
                        {
                            case PhilosopherUtils.PhilosopherAction.None:
                                if (p.State == PhilosopherUtils.PhilosopherState.Thinking)
                                {
                                    if (--p.ThinkingTimeLeft == 0)
                                    {
                                        p.EndThinking();
                                        _coordinator?.DeclareHungry(i);
                                    }
                                    break;
                                }
                                if (p.State == PhilosopherUtils.PhilosopherState.Eating)
                                {
                                    if (--p.EatingTimeLeft == 0)
                                    {
                                        p.EndEating();
                                        p.ReleaseFork(left);
                                        p.ReleaseFork(right);
                                        p.StartThinking(_rng.Next(3, 11));
                                    }
                                    break;
                                }
                                if (p.State == PhilosopherUtils.PhilosopherState.Hungry)
                                {
                                    if (p.HasLeftFork && p.HasRightFork)
                                    {
                                        p.StartEating(_rng.Next(4, 6));
                                        break;
                                    }
                                    // if the philosopher is busy, he is trying to pick the fork
                                    if (p.IsBusy && --p.TakingForkTimeLeft == 0)
                                    {
                                        if (!p.HasLeftFork)
                                        {
                                            p.FinishTakingFork(left);
                                        }
                                        else
                                        {
                                            p.FinishTakingFork(right);
                                        }
                                    }
                                }
                                break;
                            case PhilosopherUtils.PhilosopherAction.TakeLeftFork:
                                p.StartTakingFork(left);
                                break;
                            case PhilosopherUtils.PhilosopherAction.TakeRightFork:
                                p.StartTakingFork(right);
                                break;
                        }
                    }
                }
                return;
            }

            private bool DetectDeadlock(PhilosopherUtils.PhilosopherAction[] actions, bool[] isActionPossible)
            {
                if (_philos.Any(p => p.IsBusy)) return false;
                // actions.Count == isActionPossible.Count == _philos.Count
                for (int i = 0; i < _philos.Count; i++)
                {
                    var left = _forks[i];
                    var right = _forks[(i + 1) % _forks.Count];
                    if (left.Owner == right.Owner && left.Owner == _philos[i].Name)
                    {
                        return false;
                    }
                    if (actions[i] != PhilosopherUtils.PhilosopherAction.None && isActionPossible[i])
                    {
                        return false;
                    }
                }
                // No one is busy and have no actions/can't do valid action => Deadlock state
                return true;
            }

            private string GetStatusBlock()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"===== STEP {Step} =====");
                sb.AppendLine(string.Intern("Philosophers:"));
                foreach (var p in _philos)
                {
                    var st = p.State switch
                    {
                        PhilosopherUtils.PhilosopherState.Thinking => string.Intern($"Thinking ({p.ThinkingTimeLeft} steps left)"),
                        PhilosopherUtils.PhilosopherState.Hungry => string.Intern("Hungry"),
                        PhilosopherUtils.PhilosopherState.Eating => string.Intern($"Eating ({p.EatingTimeLeft} steps left)"),
                        _ => ""
                    };
                    sb.AppendLine($" {p.Name}: {st}, eaten: {p.EatenCount}");
                }
                sb.AppendLine();
                sb.AppendLine(string.Intern("Forks:"));
                foreach (var f in _forks)
                {
                    sb.AppendLine(string.Intern($" Fork-{f.Id + 1}: {f.State} {(f.Owner != null ? "(is using by " + f.Owner + ")" : "")}"));
                }
                return sb.ToString();
            }

            private string GetSummary()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("==== Final Summary ====");
                long total = _philos.Sum(p => p.EatenCount);
                sb.AppendLine($"Total eaten: {total}");
                sb.AppendLine("Per philosopher:");
                foreach (var p in _philos) sb.AppendLine($" {p.Name}: eaten={p.EatenCount}");
                sb.AppendLine();
                sb.AppendLine("==== Metrics ====");
                long totalSteps = Step == 0 ? 1 : Step;

                sb.AppendLine("Throughput (items eaten per 1000 steps):");
                double totalThroughput = 0.0;
                for (int i = 0; i < _philos.Count; i++)
                {
                    double tp = _philos[i].EatenCount * 1000.0 / totalSteps;
                    totalThroughput += tp;
                    sb.AppendLine($" {_philos[i].Name}: {tp:F2}");
                }
                sb.AppendLine($" Average: {totalThroughput / _philos.Count:F2}");

                long sumWait = _hungryTotalSteps.Sum();
                double avgWait = sumWait / (double)_philos.Count;
                long maxWait = _hungryTotalSteps.Max();
                int idxOfMaxWait = Array.IndexOf(_hungryTotalSteps, maxWait);
                sb.AppendLine($"Waiting time (steps) - average: {avgWait:F2}");
                sb.AppendLine($"Waiting time (steps) - max: {maxWait}. Philosopher: {_philos[idxOfMaxWait].Name}");



                sb.AppendLine("Fork utilization (percent of steps):");
                for (int i = 0; i < _forks.Count; i++)
                {
                    double av = _forkAvailableSteps[i] * 100.0 / totalSteps;
                    double qu = _forkQueuedSteps[i] * 100.0 / totalSteps;
                    double iu = _forkInUseSteps[i] * 100.0 / totalSteps;
                    double ea = _forkInEatingSteps[i] * 100.0 / totalSteps;
                    sb.AppendLine($" Fork-{i + 1}: Available={av:F2}%, Queued={qu:F2}%, InUse={iu:F2}%, Eating={ea:F2}%");
                }

                sb.AppendLine($"Score: {total}");
                return sb.ToString();
            }
        }
    }
}
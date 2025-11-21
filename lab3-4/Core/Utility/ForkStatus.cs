using DiningPhilosophers.Core.ForkUtils;

namespace DiningPhilosophers.Core.Utility;

public record ForkStatus(int Index, ForkState State, string? Owner);

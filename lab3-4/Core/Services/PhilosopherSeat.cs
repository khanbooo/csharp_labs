namespace DiningPhilosophers.Core.Services;

public sealed class PhilosopherSeat
{
    public PhilosopherSeat(string name, int index, int totalPhilosophers)
    {
        Name = name;
        Index = index;
        TotalPhilosophers = totalPhilosophers;
        LeftForkIndex = index;
        RightForkIndex = (index + 1) % totalPhilosophers;
    }

    public string Name { get; }
    public int Index { get; }
    public int TotalPhilosophers { get; }
    public int LeftForkIndex { get; }
    public int RightForkIndex { get; }
}

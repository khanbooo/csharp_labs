namespace DiningPhilosophers.Core.Interfaces
{
    public interface IPhilosopher
    {
        int Id { get; }
        string Name { get; }
        long EatenCount { get; }
        long WaitingMilliseconds { get; }
    }
}

namespace DiningPhilosophers.Core.Interfaces
{
    public interface ISimulation
    {
        void Run();
        Statistics.Statistics GetStatistics();
    }
}

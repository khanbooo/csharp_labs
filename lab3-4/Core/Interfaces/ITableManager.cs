using DiningPhilosophers.Core.Utility;

namespace DiningPhilosophers.Core.Interfaces;

public interface ITableManager
{
    bool TryAcquireForks(string philosopherName, int leftForkIndex, int rightForkIndex);
    void ReleaseForks(string philosopherName, int leftForkIndex, int rightForkIndex);
    void MarkEating(string philosopherName, int leftForkIndex, int rightForkIndex);
    IReadOnlyList<ForkStatus> GetStatus();
    IReadOnlyList<ForkUtilization> GetUtilization();
}

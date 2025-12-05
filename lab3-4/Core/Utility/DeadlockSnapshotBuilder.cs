using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DiningPhilosophers.Core.Services;

namespace DiningPhilosophers.Core.Utility;

public static class DeadlockSnapshotBuilder
{
    public static IReadOnlyList<PhilosopherSnapshot> BuildSnapshots(
        IReadOnlyList<PhilosopherSeat> seats,
        SimulationReport report,
        IReadOnlyList<ForkStatus> forkStatuses)
    {
        if (seats == null || seats.Count == 0 || forkStatuses == null || forkStatuses.Count == 0)
        {
            return Array.Empty<PhilosopherSnapshot>();
        }

        var summaries = report.Philosophers.ToDictionary(p => p.Name, StringComparer.Ordinal);
        var snapshots = new List<PhilosopherSnapshot>(seats.Count);

        foreach (var seat in seats)
        {
            summaries.TryGetValue(seat.Name, out var summary);
            var currentState = summary?.CurrentState ?? PhilosopherRuntimeState.Thinking;
            bool isHungry = currentState == PhilosopherRuntimeState.Hungry;

            var leftOwner = forkStatuses[seat.LeftForkIndex].Owner;
            var rightOwner = forkStatuses[seat.RightForkIndex].Owner;
            bool hasLeft = string.Equals(leftOwner, seat.Name, StringComparison.Ordinal);
            bool hasRight = string.Equals(rightOwner, seat.Name, StringComparison.Ordinal);

            snapshots.Add(new PhilosopherSnapshot(seat.Name, isHungry, hasLeft, hasRight));
        }

        return snapshots;
    }

    public static string BuildDeadlockMessage(IEnumerable<PhilosopherSnapshot> snapshots)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("DEADLOCK DETECTED");
        sb.AppendLine("All philosophers are hungry and each holds exactly one fork.");
        sb.AppendLine("Current state:");

        foreach (var snapshot in snapshots)
        {
            sb.Append("  ");
            sb.Append(snapshot.Name);
            sb.Append(": HasLeftFork=");
            sb.Append(snapshot.HasLeftFork ? "YES" : "no");
            sb.Append(", HasRightFork=");
            sb.Append(snapshot.HasRightFork ? "YES" : "no");
            sb.AppendLine();
        }

        sb.AppendLine();
        return sb.ToString();
    }
}

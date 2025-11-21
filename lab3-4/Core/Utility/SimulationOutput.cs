namespace DiningPhilosophers.Core.Utility;

public sealed class SimulationOutput
{
    public SimulationOutput(string outputPath)
    {
        OutputPath = outputPath;
        SyncRoot = new object();
    }

    public string OutputPath { get; }
    public object SyncRoot { get; }
}

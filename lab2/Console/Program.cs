using System.Text;
using DiningPhilosophers.Core.Simulation;
using DiningPhilosophers.Core.Strategies;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            string defaultInputPath = Path.Combine(@".", "resources", "names.txt");
            string defaultOutputPath = Path.Combine(@".", "resources", "output.txt");
            var namesFile = args.Length > 0 ? args[0] : defaultInputPath;
            var outputFile = args.Length > 1 ? args[1] : defaultOutputPath;

            if (!File.Exists(namesFile))
            {
                Console.WriteLine($"Names file '{namesFile}' not found. Creating default names.\n");

                Directory.CreateDirectory(Path.GetDirectoryName(namesFile) ?? ".");

                StringBuilder sb = new();
                sb.AppendLine("Платон");
                sb.AppendLine("Аристотель");
                sb.AppendLine("Сократ");
                sb.AppendLine("Декарт");
                sb.AppendLine("Кант");

                File.WriteAllText(namesFile, sb.ToString());
            }

            File.WriteAllText(outputFile, string.Empty);

            string[] lines = File.ReadAllLines(namesFile);

            var strategy = new NaiveStrategy();

            var simulation = new SimulationMultiThreaded(lines, outputFile, strategy);
            simulation.Run();

            Console.WriteLine("Simulation completed. Check output file: " + outputFile);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}\n{e.StackTrace}");
        }
    }
}

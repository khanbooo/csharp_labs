using System.Text;
using DiningPhilosophers.Core.Simulation;
using DiningPhilosophers.Core.Strategy;
using DiningPhilosophers.Core.Coordinator;

public class Program
{
    public static void Main(string[] args)
    {
        try
        {
            string defaultInputPath = Path.Combine(@".", "resources", "names.txt");
            string defaultOutputPath = Path.Combine(@".", "resources", "output.txt");
            var namesFile = args.Length > 1 ? args[1] : defaultInputPath;
            var outputFile = args.Length > 2 ? args[2] : defaultOutputPath;
            if (!File.Exists(namesFile))
            {
                try
                {
                    Console.WriteLine($"Names file '{namesFile}' not found. Using default names.\n");
                }
                catch (IOException exc)
                {
                    throw new IOException(exc.Message);
                }

                try
                {
                    File.Create(namesFile);
                }
                catch (Exception e)
                {
                    try
                    {
                        Console.WriteLine($"Can't create file, something went wrong: {e.Message}.\n");
                        return;
                    }
                    catch (IOException exc)
                    {
                        throw new IOException(exc.Message);
                    }
                }

                StringBuilder sb = new();
                try
                {
                    sb.AppendLine("Платон");
                    sb.AppendLine("Аристотель");
                    sb.AppendLine("Сократ");
                    sb.AppendLine("Декарт");
                    sb.AppendLine("Кант");
                }
                catch (ArgumentOutOfRangeException e)
                {
                    try
                    {
                        Console.WriteLine($"Can't add line to StringBuilder: {e.Message}.\n");
                        return;
                    }
                    catch (IOException exc)
                    {
                        throw new IOException(exc.Message);
                    }
                }

                try
                {
                    File.AppendAllText(namesFile, sb.ToString());
                }
                catch (Exception e)
                {
                    try
                    {
                        Console.WriteLine($"Can't add text to file: {e.Message}.\n");
                        return;
                    }
                    catch (IOException exc)
                    {
                        throw new IOException(exc.Message);
                    }
                }
            }

            try
            {
                File.Create(outputFile).Close();
            }
            catch (Exception e)
            {
                try
                {
                    Console.WriteLine($"Can't create file, something went wrong: {e.Message}.\n");
                    return;
                }
                catch (IOException exc)
                {
                    throw new IOException(exc.Message);
                }
            }

            try
            {
                string[] lines;
                try
                {
                    lines = File.ReadAllLines(namesFile);
                }
                catch (Exception e)
                {
                    try
                    {
                        Console.WriteLine($"Can't read file, something went wrong: {e.Message}.\n");
                        return;
                    }
                    catch (IOException exc)
                    {
                        throw new IOException(exc.Message);
                    }
                }
                // CLI: optional first arg is mode: "naive" or "coordinator"
                string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "naive";
                if (mode == "naive")
                {
                    NaiveStrategy strategy = new();
                    Simulation simulation = new(lines, strategy, null, outputFile);
                    simulation.RunSteps(1000000);
                }
                else if (mode == "coordinator")
                {
                    SimpleCoordinator coord = new();
                    Simulation simulation = new(lines, null, coord, outputFile);
                    simulation.RunSteps(1000000);
                }
                else
                {
                    Console.WriteLine($"Unknown mode '{mode}'. Use 'naive' or 'coordinator'.");
                    return;
                }
                return;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        catch (Exception e)
        {
            try
            {
                Console.WriteLine($"Something went wrong: {e.Message}\n");
                return;
            }
            catch (IOException exc)
            {
                throw new IOException(exc.Message);
            }
        }
    }
}

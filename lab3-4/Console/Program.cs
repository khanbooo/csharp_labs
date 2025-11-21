using System.Text;
using System.Linq;
using DiningPhilosophers.Core.Hosting;
using DiningPhilosophers.Core.Interfaces;
using DiningPhilosophers.Core.Options;
using DiningPhilosophers.Core.Services;
using DiningPhilosophers.Core.Strategies;
using DiningPhilosophers.Core.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class Program
{
    public static async Task Main(string[] args)
    {
        var resourcesDirectory = ResolveResourcesDirectory();
        var defaultInputPath = Path.Combine(resourcesDirectory, "names.txt");
        var defaultOutputPath = Path.Combine(resourcesDirectory, "output.txt");

        var namesFile = args.Length > 0 ? args[0] : defaultInputPath;
        var outputFile = args.Length > 1 ? args[1] : defaultOutputPath;

        var philosopherNames = EnsureNamesFile(namesFile);
        Console.WriteLine($"Loaded {philosopherNames.Count} philosophers from {namesFile}.");
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? ".");
        File.WriteAllText(outputFile, string.Empty);

        var builder = Host.CreateApplicationBuilder(args);
        var appsettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        builder.Configuration.AddJsonFile(appsettingsPath, optional: false, reloadOnChange: true);

        builder.Services.Configure<SimulationOptions>(builder.Configuration.GetSection("Simulation"));
        builder.Services.AddSingleton<IMetricsCollector>(_ =>
        {
            var collector = new MetricsCollector();
            foreach (var name in philosopherNames)
            {
                collector.RegisterPhilosopher(name);
            }
            return collector;
        });
        builder.Services.AddSingleton<IPhilosopherStrategy, LeftRightStrategy>();
        builder.Services.AddSingleton<ITableManager>(_ => new TableManager(philosopherNames.Count));
        builder.Services.AddSingleton(new SimulationOutput(outputFile));
        builder.Services.AddHostedService<SimulationSupervisor>();
        builder.Services.AddHostedService<StatusReporter>();

        for (int i = 0; i < philosopherNames.Count; i++)
        {
            var seat = new PhilosopherSeat(philosopherNames[i], i, philosopherNames.Count);
            builder.Services.AddSingleton<IHostedService>(sp => new PhilosopherHostedService(
                seat,
                sp.GetRequiredService<IPhilosopherStrategy>(),
                sp.GetRequiredService<ITableManager>(),
                sp.GetRequiredService<IMetricsCollector>(),
                sp.GetRequiredService<IOptions<SimulationOptions>>(),
                sp.GetRequiredService<ILogger<PhilosopherHostedService>>()));
        }

        using var host = builder.Build();
        await host.RunAsync();
    }

    private static List<string> EnsureNamesFile(string namesFile)
    {
        if (!File.Exists(namesFile))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(namesFile) ?? ".");

            var sb = new StringBuilder();
            sb.AppendLine("Платон");
            sb.AppendLine("Аристотель");
            sb.AppendLine("Сократ");
            sb.AppendLine("Декарт");
            sb.AppendLine("Кант");
            File.WriteAllText(namesFile, sb.ToString());
        }

        return File.ReadAllLines(namesFile)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToList();
    }

    private static string ResolveResourcesDirectory()
    {
        var rootCandidate = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "resources"));
        if (Directory.Exists(rootCandidate))
        {
            return rootCandidate;
        }

        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "resources");
        if (Directory.Exists(cwdCandidate))
        {
            return cwdCandidate;
        }

        Directory.CreateDirectory(rootCandidate);
        return rootCandidate;
    }
}

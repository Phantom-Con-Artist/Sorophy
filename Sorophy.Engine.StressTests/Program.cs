using System.Globalization;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = StressRunOptions.Parse(args);

            if (options.ShowHelp)
            {
                PrintHelp();
                return 0;
            }

            var runner = new TestArsenalRunner();

            if (options.Command == "list")
            {
                runner.PrintCampaigns();
                return 0;
            }

            var result = options.Command == "all"
                ? runner.RunAll(options)
                : runner.RunSingle(options.Command, options);

            return result.OverallPass ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                  TEST ARSENAL CRASHED                      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine(exception);
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Sorophy™ Test Arsenal");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  all                     Run every registered campaign");
        Console.WriteLine("  list                    List discovered campaigns");
        Console.WriteLine("  replay                  Run deterministic replay only");
        Console.WriteLine("  mutation                Run mutation chaos only");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --seed <int>            Base deterministic seed");
        Console.WriteLine("  --operations <int>      Operations per campaign");
        Console.WriteLine("  --audit <int>           Audit interval");
        Console.WriteLine("  --profile <name>        quick | full | extreme");
        Console.WriteLine("  --output <path>         Results directory");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run --project Sorophy.Engine.StressTests -- all");
        Console.WriteLine("  dotnet run --project Sorophy.Engine.StressTests -- all --profile full");
        Console.WriteLine("  dotnet run --project Sorophy.Engine.StressTests -- replay --seed 12345 --operations 10000");
        Console.WriteLine("  dotnet run --project Sorophy.Engine.StressTests -- list");
        Console.WriteLine();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Sorophy.Engine.StressTests.Infrastructure;

public sealed class TestArsenalRunner
{
    private readonly IReadOnlyList<CampaignDefinition> _campaigns;

    public TestArsenalRunner()
    {
        _campaigns =
            Assembly.GetExecutingAssembly()
                .GetTypes()
                .Select(type =>
                {
                    var attribute =
                        type.GetCustomAttribute<TestCampaignAttribute>();

                    if (attribute is null)
                    {
                        return null;
                    }

                    var method =
                        type.GetMethod(
                            "Run",
                            BindingFlags.Public |
                            BindingFlags.Static);

                    if (method is null)
                    {
                        throw new InvalidOperationException(
                            $"Campaign '{type.FullName}' has a " +
                            "TestCampaignAttribute but no public static Run method.");
                    }

                    if (method.ReturnType != typeof(TestCampaignResult))
                    {
                        throw new InvalidOperationException(
                            $"Campaign '{type.FullName}' must return TestCampaignResult.");
                    }

                    var parameters = method.GetParameters();

                    if (parameters.Length != 1 ||
                        parameters[0].ParameterType !=
                        typeof(TestCampaignContext))
                    {
                        throw new InvalidOperationException(
                            $"Campaign '{type.FullName}' must expose " +
                            "Run(TestCampaignContext).");
                    }

                    return new CampaignDefinition(
                        attribute.Name,
                        attribute.Command,
                        attribute.Order,
                        method);
                })
                .Where(x => x is not null)
                .Cast<CampaignDefinition>()
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    public void PrintCampaigns()
    {
        Console.WriteLine();
        Console.WriteLine("SOROPHY TEST ARSENAL");
        Console.WriteLine("========================");
        Console.WriteLine();

        foreach (var campaign in _campaigns)
        {
            Console.WriteLine(
                $"[{campaign.Order,3}] " +
                $"{campaign.Command,-18} " +
                $"{campaign.Name}");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Discovered campaigns: {_campaigns.Count}");
        Console.WriteLine();
    }

    public TestArsenalResult RunAll(
        StressRunOptions options)
    {
        return RunCampaigns(
            _campaigns,
            options);
    }

    public TestArsenalResult RunSingle(
        string command,
        StressRunOptions options)
    {
        var campaign =
            _campaigns.FirstOrDefault(
                x =>
                    string.Equals(
                        x.Command,
                        command,
                        StringComparison.OrdinalIgnoreCase));

        if (campaign is null)
        {
            throw new ArgumentException(
                $"Unknown campaign '{command}'. " +
                "Run 'list' to see discovered campaigns.");
        }

        return RunCampaigns(
            new[] { campaign },
            options);
    }

    private TestArsenalResult RunCampaigns(
        IReadOnlyList<CampaignDefinition> campaigns,
        StressRunOptions options)
    {
        if (campaigns.Count == 0)
        {
            throw new InvalidOperationException(
                "No stress campaigns were discovered.");
        }

        var started =
            DateTimeOffset.Now;

        var context =
            TestCampaignContext.From(options);

        Console.WriteLine();
        Console.WriteLine(
            "╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine(
            "║                 SOROPHY TEST ARSENAL                     ║");
        Console.WriteLine(
            "╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine(
            $"║ Seed:       {options.Seed,-45}║");
        Console.WriteLine(
            $"║ Profile:    {options.Profile,-45}║");
        Console.WriteLine(
            $"║ Operations: {options.Operations,-45}║");
        Console.WriteLine(
            $"║ Campaigns:  {campaigns.Count,-45}║");
        Console.WriteLine(
            "╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        var results =
            new List<TestCampaignResult>();

        for (var index = 0;
             index < campaigns.Count;
             index++)
        {
            var campaign =
                campaigns[index];

            Console.WriteLine(
                $"[{index + 1:00}/{campaigns.Count:00}] " +
                $"{campaign.Name} ...");

            var stopwatch =
                Stopwatch.StartNew();

            TestCampaignResult result;

            try
            {
                result =
                    (TestCampaignResult)
                        campaign.RunMethod.Invoke(
                            null,
                            new object[] { context })!;
            }
            catch (TargetInvocationException exception)
            {
                var inner =
                    exception.InnerException ??
                    exception;

                result =
                    TestCampaignResult.Fail(
                        campaign.Name,
                        campaign.Command,
                        stopwatch.Elapsed,
                        inner.Message,
                        inner);
            }
            catch (Exception exception)
            {
                result =
                    TestCampaignResult.Fail(
                        campaign.Name,
                        campaign.Command,
                        stopwatch.Elapsed,
                        exception.Message,
                        exception);
            }

            stopwatch.Stop();

            if (result.Duration == TimeSpan.Zero)
            {
                result =
                    result with
                    {
                        Duration = stopwatch.Elapsed
                    };
            }

            results.Add(result);

            Console.WriteLine(
                result.Success
                    ? $"    PASS  {result.PassedChecks}/{result.TotalChecks} checks  " +
                      $"{result.Duration.TotalSeconds:F3}s"
                    : $"    FAIL  {result.PassedChecks}/{result.TotalChecks} checks  " +
                      $"{result.Duration.TotalSeconds:F3}s");

            if (!result.Success &&
                !string.IsNullOrWhiteSpace(result.Message))
            {
                Console.WriteLine(
                    $"    {result.Message}");
            }

            Console.WriteLine();
        }

        var finished =
            DateTimeOffset.Now;

        var arsenal =
            new TestArsenalResult(
                started,
                finished,
                options,
                results);

        TestResultWriter.Write(arsenal);

        PrintSummary(arsenal);

        return arsenal;
    }

    private static void PrintSummary(
        TestArsenalResult result)
    {
        Console.WriteLine();
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine(
            "                    TEST ARSENAL RESULT");
        Console.WriteLine(
            "════════════════════════════════════════════════════════════════");
        Console.WriteLine();

        Console.WriteLine(
            $"Campaigns: {result.PassedCampaigns}/{result.TotalCampaigns} passed");

        Console.WriteLine(
            $"Checks:    {result.PassedChecks}/{result.TotalChecks} passed");

        Console.WriteLine(
            $"Duration:  {(result.FinishedAt - result.StartedAt)}");

        Console.WriteLine();

        if (result.OverallPass)
        {
            Console.WriteLine("                    🟢 PASS");
        }
        else
        {
            Console.WriteLine("                    🔴 FAIL");
        }

        Console.WriteLine();
        Console.WriteLine(
            $"Results recorded in: {result.Options.OutputDirectory}");
        Console.WriteLine();
    }

    private sealed record CampaignDefinition(
        string Name,
        string Command,
        int Order,
        MethodInfo RunMethod);
}

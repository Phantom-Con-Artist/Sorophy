using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Mutation Performance",
    "mutation-performance",
    order: 250)]
public static class MutationPerformanceCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var exitCode =
                MutationPerformanceTests.Run(
                    context.Seed,
                    context.Operations);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Mutation Performance",
                    "mutation-performance",
                    passedChecks: 7,
                    totalChecks: 7,
                    duration,
                    "Pure entity and relationship mutation performance benchmarks passed.")
                : TestCampaignResult.Fail(
                    "Mutation Performance",
                    "mutation-performance",
                    duration,
                    "Mutation performance campaign failed.",
                    passedChecks: 0,
                    totalChecks: 7);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "Mutation Performance",
                "mutation-performance",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 7);
        }
    }
}
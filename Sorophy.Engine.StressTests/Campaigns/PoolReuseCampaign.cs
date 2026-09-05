using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Pool Reuse",
    "pool-reuse",
    order: 275)]
public static class PoolReuseCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var exitCode =
                PoolReuseTests.Run(
                    context.Seed,
                    context.Operations);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Pool Reuse",
                    "pool-reuse",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "Adjacency slab storage survived repeated allocation/removal cycles with reuse evidence and final graph validation.")
                : TestCampaignResult.Fail(
                    "Pool Reuse",
                    "pool-reuse",
                    duration,
                    "Pool reuse campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "Pool Reuse",
                "pool-reuse",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
using System;
using Orb.Engine.StressTests.Infrastructure;

namespace Orb.Engine.StressTests;

[TestCampaign(
    "High-Degree Topology",
    "high-degree",
    order: 290)]
public static class HighDegreeTopologyCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var exitCode =
                HighDegreeTopologyTests.Run(
                    context.Seed,
                    context.Operations);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "High-Degree Topology",
                    "high-degree",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "High-degree hub, parallel relationships, self-links, queries, removal, and final validation passed.")
                : TestCampaignResult.Fail(
                    "High-Degree Topology",
                    "high-degree",
                    duration,
                    "High-degree topology campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "High-Degree Topology",
                "high-degree",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
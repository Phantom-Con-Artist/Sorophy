using System;
using Orb.Engine.StressTests.Infrastructure;

namespace Orb.Engine.StressTests;

[TestCampaign(
    "Serialization Torture",
    "serialization-torture",
    order: 700)]
public static class SerializationTortureCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var exitCode =
                SerializationTortureTests.Run(
                    context.Seed,
                    context.Operations);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Serialization Torture",
                    "serialization-torture",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "Entity and graph serialization survived round trips, deterministic output checks, filesystem I/O, large graph scales, and malformed-input attacks.")
                : TestCampaignResult.Fail(
                    "Serialization Torture",
                    "serialization-torture",
                    duration,
                    "Serialization torture campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "Serialization Torture",
                "serialization-torture",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
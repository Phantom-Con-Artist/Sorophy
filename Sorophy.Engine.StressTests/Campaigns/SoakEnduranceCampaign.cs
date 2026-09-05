using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Soak / Endurance",
    "soak",
    order: 900)]
public static class SoakEnduranceCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var auditInterval =
                context.AuditInterval;

            var exitCode =
                SoakEnduranceTests.Run(
                    context.Seed,
                    context.Operations,
                    auditInterval);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Soak / Endurance",
                    "soak",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "Repeated mutation, query, validation, in-memory persistence, disk persistence, pool reuse, canonical-state verification, and resource monitoring survived the requested soak workload.")
                : TestCampaignResult.Fail(
                    "Soak / Endurance",
                    "soak",
                    duration,
                    "Soak / endurance campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "Soak / Endurance",
                "soak",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
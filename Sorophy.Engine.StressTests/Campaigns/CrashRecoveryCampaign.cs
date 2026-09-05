using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Crash / Recovery Torture",
    "crash-recovery",
    order: 800)]
public static class CrashRecoveryCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var auditInterval =
                Math.Clamp(
                    context.Operations / 100,
                    1_000,
                    10_000);

            var exitCode =
                CrashRecoveryTests.Run(
                    context.Seed,
                    context.Operations,
                    auditInterval);

            var duration =
                DateTime.UtcNow -
                started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Crash / Recovery Torture",
                    "crash-recovery",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "Crash/recovery contract checks and the requested endurance workload passed.")
                : TestCampaignResult.Fail(
                    "Crash / Recovery Torture",
                    "crash-recovery",
                    duration,
                    "Crash/recovery torture campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow -
                started;

            return TestCampaignResult.Fail(
                "Crash / Recovery Torture",
                "crash-recovery",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
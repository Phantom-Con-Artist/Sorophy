using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Differential Fuzzing",
    "differential-fuzz",
    order: 600)]
public static class DifferentialFuzzingCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started =
            DateTime.UtcNow;

        try
        {
            var exitCode =
                DifferentialFuzzingTests.Run(
                    context.Seed,
                    context.Operations,
                    context.AuditInterval,
                    context.Profile);

            var duration =
                DateTime.UtcNow - started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Differential Fuzzing",
                    "differential-fuzz",
                    passedChecks: 1,
                    totalChecks: 1,
                    duration,
                    "Deterministic randomized graph workloads matched an independent reference model across all configured seed families.")
                : TestCampaignResult.Fail(
                    "Differential Fuzzing",
                    "differential-fuzz",
                    duration,
                    "Differential fuzzing campaign failed.",
                    passedChecks: 0,
                    totalChecks: 1);
        }
        catch (Exception exception)
        {
            var duration =
                DateTime.UtcNow - started;

            return TestCampaignResult.Fail(
                "Differential Fuzzing",
                "differential-fuzz",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 1);
        }
    }
}
using System;
using Sorophy.Engine.StressTests.Infrastructure;

namespace Sorophy.Engine.StressTests;

[TestCampaign(
    "Deterministic Replay",
    "replay",
    order: 200)]
public static class DeterministicReplayCampaign
{
    public static TestCampaignResult Run(
        TestCampaignContext context)
    {
        var started = DateTime.UtcNow;

        try
        {
            var exitCode =
                DeterministicReplayTests.Run(
                    context.Seed,
                    context.Operations,
                    context.AuditInterval);

            var duration = DateTime.UtcNow - started;

            return exitCode == 0
                ? TestCampaignResult.Pass(
                    "Deterministic Replay",
                    "replay",
                    passedChecks: 4,
                    totalChecks: 4,
                    duration,
                    "Journal determinism, reference model, replay equivalence, and graph invariants passed.")
                : TestCampaignResult.Fail(
                    "Deterministic Replay",
                    "replay",
                    duration,
                    "Deterministic replay campaign failed.",
                    passedChecks: 0,
                    totalChecks: 4);
        }
        catch (Exception exception)
        {
            var duration = DateTime.UtcNow - started;

            return TestCampaignResult.Fail(
                "Deterministic Replay",
                "replay",
                duration,
                exception.Message,
                exception,
                passedChecks: 0,
                totalChecks: 4);
        }
    }
}

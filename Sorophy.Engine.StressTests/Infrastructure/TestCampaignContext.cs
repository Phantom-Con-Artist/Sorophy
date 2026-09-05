using System;

namespace Sorophy.Engine.StressTests.Infrastructure;

public sealed record TestCampaignContext(
    int Seed,
    int Operations,
    int AuditInterval,
    string Profile,
    string ResultsDirectory)
{
    public static TestCampaignContext From(
        StressRunOptions options)
    {
        return new TestCampaignContext(
            options.Seed,
            checked((int)options.Operations),
            options.AuditInterval,
            options.Profile,
            options.OutputDirectory);
    }
}

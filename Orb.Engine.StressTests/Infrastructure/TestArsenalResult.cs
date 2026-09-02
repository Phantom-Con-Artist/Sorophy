using System;
using System.Collections.Generic;
using System.Linq;

namespace Orb.Engine.StressTests.Infrastructure;

public sealed record TestArsenalResult(
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    StressRunOptions Options,
    IReadOnlyList<TestCampaignResult> Campaigns)
{
    public bool OverallPass =>
        Campaigns.Count > 0 &&
        Campaigns.All(x => x.Success);

    public int PassedCampaigns =>
        Campaigns.Count(x => x.Success);

    public int TotalCampaigns =>
        Campaigns.Count;

    public int PassedChecks =>
        Campaigns.Sum(x => x.PassedChecks);

    public int TotalChecks =>
        Campaigns.Sum(x => x.TotalChecks);
}

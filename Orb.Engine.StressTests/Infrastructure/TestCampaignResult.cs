using System;

namespace Orb.Engine.StressTests.Infrastructure;

public enum CampaignStatus
{
    Passed,
    Failed
}

public sealed record TestCampaignResult(
    string Name,
    string Command,
    CampaignStatus Status,
    int PassedChecks,
    int TotalChecks,
    TimeSpan Duration,
    string? Message = null,
    string? ExceptionType = null)
{
    public bool Success =>
        Status == CampaignStatus.Passed &&
        PassedChecks == TotalChecks;

    public static TestCampaignResult Pass(
        string name,
        string command,
        int passedChecks,
        int totalChecks,
        TimeSpan duration,
        string? message = null)
    {
        return new TestCampaignResult(
            name,
            command,
            CampaignStatus.Passed,
            passedChecks,
            totalChecks,
            duration,
            message);
    }

    public static TestCampaignResult Fail(
        string name,
        string command,
        TimeSpan duration,
        string message,
        Exception? exception = null,
        int passedChecks = 0,
        int totalChecks = 1)
    {
        return new TestCampaignResult(
            name,
            command,
            CampaignStatus.Failed,
            passedChecks,
            totalChecks,
            duration,
            message,
            exception?.GetType().FullName);
    }
}

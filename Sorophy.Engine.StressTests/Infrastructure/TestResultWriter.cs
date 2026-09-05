using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Sorophy.Engine.StressTests.Infrastructure;

public static class TestResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static void Write(
        TestArsenalResult result)
    {
        var directory =
            Path.GetFullPath(
                result.Options.OutputDirectory);

        Directory.CreateDirectory(directory);

        var archiveDirectory =
            Path.Combine(
                directory,
                "archive");

        Directory.CreateDirectory(
            archiveDirectory);

        var timestamp =
            result.StartedAt.ToLocalTime()
                .ToString(
                    "yyyyMMdd-HHmmss",
                    System.Globalization.CultureInfo.InvariantCulture);

        var json =
            JsonSerializer.Serialize(
                result,
                JsonOptions);

        var text =
            BuildTextReport(result);

        File.WriteAllText(
            Path.Combine(directory, "latest.json"),
            json,
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(directory, "latest.txt"),
            text,
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(
                archiveDirectory,
                $"{timestamp}.json"),
            json,
            Encoding.UTF8);

        File.WriteAllText(
            Path.Combine(
                archiveDirectory,
                $"{timestamp}.txt"),
            text,
            Encoding.UTF8);
    }

    private static string BuildTextReport(
        TestArsenalResult result)
    {
        var builder =
            new StringBuilder();

        builder.AppendLine(
            "SOROPHY ENGINE TEST ARSENAL REPORT");

        builder.AppendLine(
            "================================");

        builder.AppendLine();

        builder.AppendLine(
            $"Started:     {result.StartedAt}");

        builder.AppendLine(
            $"Finished:    {result.FinishedAt}");

        builder.AppendLine(
            $"Seed:        {result.Options.Seed}");

        builder.AppendLine(
            $"Profile:     {result.Options.Profile}");

        builder.AppendLine(
            $"Operations:  {result.Options.Operations}");

        builder.AppendLine(
            $"Audit:       {result.Options.AuditInterval}");

        builder.AppendLine();

        builder.AppendLine(
            "CAMPAIGNS");

        builder.AppendLine(
            "---------");

        foreach (var campaign in result.Campaigns)
        {
            builder.AppendLine(
                $"{campaign.Status,-8} " +
                $"{campaign.PassedChecks,6}/{campaign.TotalChecks,-6} " +
                $"{campaign.Name,-30} " +
                $"{campaign.Duration.TotalSeconds,10:F3}s");

            if (!string.IsNullOrWhiteSpace(
                    campaign.Message))
            {
                builder.AppendLine(
                    $"          {campaign.Message}");
            }
        }

        builder.AppendLine();

        builder.AppendLine(
            "SUMMARY");

        builder.AppendLine(
            "-------");

        builder.AppendLine(
            $"Campaigns: {result.PassedCampaigns}/{result.TotalCampaigns}");

        builder.AppendLine(
            $"Checks:    {result.PassedChecks}/{result.TotalChecks}");

        builder.AppendLine(
            $"Status:    {(result.OverallPass ? "PASS" : "FAIL")}");

        builder.AppendLine();

        return builder.ToString();
    }
}

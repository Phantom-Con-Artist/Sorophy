using System;
using System.Globalization;

namespace Orb.Engine.StressTests.Infrastructure;

public sealed record StressRunOptions(
    string Command,
    int Seed,
    long Operations,
    int AuditInterval,
    string Profile,
    string OutputDirectory,
    bool ShowHelp)
{
    public static StressRunOptions Parse(string[] args)
    {
        var command = "all";
        var seed = 12345;
        long operations = 10_000;
        var auditInterval = 1_000;
        var profile = "quick";
        var outputDirectory = "Results";
        var showHelp = false;

        var index = 0;

        if (args.Length > 0 &&
            !args[0].StartsWith("-", StringComparison.Ordinal))
        {
            command = args[0].Trim().ToLowerInvariant();
            index = 1;
        }

        for (; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--seed":
                    seed = int.Parse(
                        GetValue(args, ref index),
                        CultureInfo.InvariantCulture);
                    break;

                case "--operations":
                    operations = long.Parse(
                        GetValue(args, ref index),
                        CultureInfo.InvariantCulture);
                    break;

                case "--audit":
                    auditInterval = int.Parse(
                        GetValue(args, ref index),
                        CultureInfo.InvariantCulture);
                    break;

                case "--profile":
                    profile = GetValue(args, ref index)
                        .Trim()
                        .ToLowerInvariant();
                    break;

                case "--output":
                    outputDirectory = GetValue(args, ref index);
                    break;

                case "--help":
                case "-h":
                    showHelp = true;
                    break;

                default:
                    throw new ArgumentException(
                        $"Unknown argument '{args[index]}'.");
            }
        }

        if (operations <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "Operations must be greater than zero.");
        }

        if (operations > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(operations),
                "The current campaign APIs accept an Int32 operation count.");
        }

        if (auditInterval <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(auditInterval),
                "Audit interval must be greater than zero.");
        }

        if (profile is not ("quick" or "full" or "extreme"))
        {
            throw new ArgumentException(
                "Profile must be quick, full, or extreme.");
        }

        return new StressRunOptions(
            command,
            seed,
            operations,
            auditInterval,
            profile,
            outputDirectory,
            showHelp);
    }

    private static string GetValue(
        string[] args,
        ref int index)
    {
        index++;

        if (index >= args.Length)
        {
            throw new ArgumentException(
                $"Missing value after '{args[index - 1]}'.");
        }

        return args[index];
    }
}

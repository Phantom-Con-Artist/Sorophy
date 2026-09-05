using System;

namespace Sorophy.Engine.StressTests.Infrastructure;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class TestCampaignAttribute : Attribute
{
    public TestCampaignAttribute(
        string name,
        string command,
        int order = 0)
    {
        Name = name;
        Command = command;
        Order = order;
    }

    public string Name { get; }

    public string Command { get; }

    public int Order { get; }
}

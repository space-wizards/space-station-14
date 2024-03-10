using System;
using System.Linq;
using Content.Shared.Database;
using NUnit.Framework;

namespace Content.Tests.Shared.Administration.Logs;

[TestFixture, TestOf(typeof(LogType)), Parallelizable(ParallelScope.All)]
public sealed class LogTypeTests
{
    [Test]
    public void Unique()
    {
        var logTypes = Enum.GetValues<LogType>();
        var uniqueValuesCount = logTypes.ToHashSet().Count;

        Assert.That(logTypes.Count, Is.EqualTo(uniqueValuesCount), $"Detected duplicate values in {nameof(LogType)} Enum.");
    }
}

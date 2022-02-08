using System;
using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using NUnit.Framework;

namespace Content.Tests.Shared.Administration.Logs;

[TestFixture]
public class LogTypeTests
{
    [Test]
    public void Unique()
    {
        var types = Enum.GetValues<LogType>();
        var duplicates = types
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        Assert.That(duplicates.Length, Is.Zero, $"{nameof(LogType)} has duplicate values for: " + string.Join(", ", duplicates));
    }
}

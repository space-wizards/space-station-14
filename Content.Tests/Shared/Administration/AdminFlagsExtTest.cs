using Content.Shared.Administration;
using NUnit.Framework;
using System.Collections.Generic;

namespace Content.Tests.Shared.Administration;

[TestFixture, TestOf(typeof(AdminFlags)), Parallelizable(ParallelScope.All)]
public sealed class AdminFlagsExtTest
{
    public static readonly IEnumerable<(string[] names, AdminFlags flags)> TestData = new[]
    {
        (System.Array.Empty<string>(), AdminFlags.None),
        (new string[1] { "ADMIN" }, AdminFlags.Admin),
        (new string[2] { "ADMIN", "DEBUG" }, AdminFlags.Admin | AdminFlags.Debug),
        (new string[3] { "ADMIN", "DEBUG", "HOST" }, AdminFlags.Admin | AdminFlags.Debug | AdminFlags.Host)
    };

    [Test]
    public void TestNamesToFlags([ValueSource(nameof(TestData))] (string[] names, AdminFlags flags) data)
    {
        Assert.That(AdminFlagsHelper.NamesToFlags(data.names), Is.EqualTo(data.flags));
    }

    [Test]
    public void TestFlagsToNames([ValueSource(nameof(TestData))] (string[] names, AdminFlags flags) data)
    {
        Assert.That(AdminFlagsHelper.FlagsToNames(data.flags), Is.EquivalentTo(data.names));
    }
}

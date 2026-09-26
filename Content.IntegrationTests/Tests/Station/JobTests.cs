#nullable enable
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;

namespace Content.IntegrationTests.Tests.Station;

[TestOf(typeof(SharedJobSystem))]
public sealed class JobTest : GameTest
{
    private static readonly string[] JobPrototypes = GameDataScrounger.PrototypesOfKind<JobPrototype>();

    /// <summary>
    /// Ensures that every job belongs to at most 1 primary department.
    /// Having no primary department is ok.
    /// </summary>
    [Test]
    [TestCaseSource(nameof(JobPrototypes))]
    [Description("Ensures that every job belongs to at most 1 primary department.")]
    [RunOnSide(Side.Server)]
    public async Task PrimaryDepartmentsTest(string jobId)
    {
        // Only checking primary departments, so don't bother with others.
        // Not actually using the jobs system since that will return the first department
        // and we need to test that there is never more than 1, so it not sorting them is correct.
        var departments = SProtoMan.EnumeratePrototypes<DepartmentPrototype>()
            .Where(department => department.Primary && department.Roles.Contains(jobId))
            .Select(department => department.ID)
            .ToList();

        Assert.That(departments, Has.Count.LessThanOrEqualTo(1), $"{jobId} belongs to multiple primary departments: {string.Join(", ", departments)}");
    }
}

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Utility;
using Content.Shared.Contraband;

namespace Content.IntegrationTests.Tests;

public sealed class ContrabandTest : GameTest
{
    private static readonly string[] ContrabandEntities = GameDataScrounger.EntitiesWithComponent("Contraband");

    [TestCaseSource(nameof(ContrabandEntities))]
    [Description($"Checks that an entity with a {nameof(ContrabandComponent)} is configured correctly")]
    [RunOnSide(Side.Client)]
    public async Task EntityShowDepartmentsAndJobs(string protoId)
    {
        var proto = CProtoMan.Index(protoId);

        if (!proto.TryComp<ContrabandComponent>(out var contraband, CEntMan.ComponentFactory))
            return;

        if (!CProtoMan.TryIndex(contraband.Severity, out var severity))
        {
            Assert.Fail($"{proto.ID} has a {nameof(ContrabandComponent)} with an unknown severity.");
        }

        if (!severity.ShowDepartmentsAndJobs)
            return;

        Assert.That(contraband.AllowedDepartments.Count + contraband.AllowedJobs.Count, Is.Not.Zero,
            @$"{protoId} has a {nameof(ContrabandComponent)} with {nameof(ContrabandSeverityPrototype.ShowDepartmentsAndJobs)} but no allowed departments or jobs.");
    }
}

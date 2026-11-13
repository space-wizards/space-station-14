#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Contraband;

namespace Content.IntegrationTests.Tests;

public sealed class ContrabandTest : GameTest
{
    [Test]
    [Description($"Checks that an entity with a {nameof(ContrabandComponent)} is configured correctly.")]
    [RunOnSide(Side.Client)]
    public async Task EntityShowDepartmentsAndJobs()
    {
        using (Assert.EnterMultipleScope())
        {
            foreach (var (proto, contraband) in Pair.GetPrototypesWithComponent<ContrabandComponent>())
            {
                Assert.That(CProtoMan.TryIndex(contraband.Severity, out var severity),
                    $"{proto.ID} has a {nameof(ContrabandComponent)} with an unknown severity."
                );

                if (!severity!.ShowDepartmentsAndJobs)
                    return;

                Assert.That(contraband.AllowedDepartments.Count + contraband.AllowedJobs.Count, Is.Not.Zero,
                    @$"{proto.ID} has a {nameof(ContrabandComponent)} with {nameof(ContrabandSeverityPrototype.ShowDepartmentsAndJobs)} but no allowed departments or jobs.");
            }
        }
    }
}

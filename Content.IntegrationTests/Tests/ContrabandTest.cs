using Content.Shared.Contraband;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class ContrabandTest
{
    [Test]
    public async Task EntityShowDepartmentsAndJobs()
    {
        await using var pair = await PoolManager.GetServerClient();
        var client = pair.Client;
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var componentFactory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (proto.Abstract || pair.IsTestPrototype(proto))
                        continue;

                    if (!proto.TryGetComponent<ContrabandComponent>(out var contraband, componentFactory))
                        continue;

                    if (!protoMan.TryIndex(contraband.Severity, out var severity))
                    {
                        Assert.Fail($"{proto.ID} has a ContrabandComponent with a unknown severity.");
                        continue;
                    }

                    if (!severity.ShowDepartmentsAndJobs)
                        continue;

                    Assert.That(contraband.AllowedDepartments.Count + contraband.AllowedJobs.Count, Is.Not.EqualTo(0),
                        @$"{proto.ID} has a ContrabandComponent with ShowDepartmentsAndJobs but no allowed departments or jobs.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

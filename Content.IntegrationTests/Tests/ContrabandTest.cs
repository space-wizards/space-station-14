using System.Linq;
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
            var protos = protoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract)
                .Where(p => !pair.IsTestPrototype(p))
                .Where(p => p.TryGetComponent<ContrabandComponent>(out _, componentFactory))
                .OrderBy(p => p.ID)
                .ToList();

            foreach (var proto in protos)
            {
                Assert.That(proto.TryGetComponent<ContrabandComponent>(out var contraband, componentFactory));
                Assert.That(protoMan.TryIndex(contraband.Severity, out var severity));

                var list = contraband.AllowedDepartments.Select(p => p.Id).Concat(contraband.AllowedJobs.Select(p => p.Id)).ToList();
                if (severity.ShowDepartmentsAndJobs)
                    Assert.That(list, Is.Not.Empty, @$"{proto.ID} has a ContrabandComponent with ShowDepartmentsAndJobs but no allowed departments or jobs.");
            }
        });

        await pair.CleanReturnAsync();
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using NUnit.Framework;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class ResearchTest
{
    [Test]
    public async Task AllTechPrintableTest()
    {
        await using var pairTracker = await PoolManager.GetServerClient(new PoolSettings {NoClient = true});
        var server = pairTracker.Pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var allEnts = protoManager.EnumeratePrototypes<EntityPrototype>();
            var allLathes = new HashSet<LatheComponent>();
            foreach (var proto in allEnts)
            {
                if (proto.Abstract)
                    continue;

                if (!proto.TryGetComponent<LatheComponent>(out var lathe))
                    continue;
                allLathes.Add(lathe);
            }

            var latheTechs = new HashSet<string>();
            foreach (var lathe in allLathes)
            {
                if (lathe.DynamicRecipes == null)
                    continue;

                foreach (var recipe in lathe.DynamicRecipes)
                {
                    if (!latheTechs.Contains(recipe))
                        latheTechs.Add(recipe);
                }
            }

            foreach (var tech in protoManager.EnumeratePrototypes<TechnologyPrototype>())
            {
                foreach (var recipe in tech.UnlockedRecipes)
                {
                    Assert.That(latheTechs, Does.Contain(recipe), $"Recipe \"{recipe}\" cannot be unlocked on any lathes.");
                }
            }
        });

        await pairTracker.CleanReturnAsync();
    }
}

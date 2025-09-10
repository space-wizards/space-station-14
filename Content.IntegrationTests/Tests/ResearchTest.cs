using System.Collections.Generic;
using System.Linq;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class ResearchTest
{
    [Test]
    public async Task DisciplineValidTierPrerequesitesTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();

        await server.WaitAssertion(() =>
        {
            var allTechs = protoManager.EnumeratePrototypes<TechnologyPrototype>().ToList();

            Assert.Multiple(() =>
            {
                foreach (var discipline in protoManager.EnumeratePrototypes<TechDisciplinePrototype>())
                {
                    foreach (var tech in allTechs)
                    {
                        if (tech.Discipline != discipline.ID)
                            continue;

                        // we ignore these, anyways
                        if (tech.Tier == 1)
                            continue;

                        Assert.That(tech.Tier, Is.GreaterThan(0), $"Technology {tech} has invalid tier {tech.Tier}.");
                        Assert.That(discipline.TierPrerequisites.ContainsKey(tech.Tier),
                            $"Discipline {discipline.ID} does not have a TierPrerequisites definition for tier {tech.Tier}");
                    }
                }
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AllTechPrintableTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var compFact = server.ResolveDependency<IComponentFactory>();

        var latheSys = entMan.System<SharedLatheSystem>();

        await server.WaitAssertion(() =>
        {
            var allEnts = protoManager.EnumeratePrototypes<EntityPrototype>();
            var latheTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
            foreach (var proto in allEnts)
            {
                if (proto.Abstract)
                    continue;

                if (pair.IsTestPrototype(proto))
                    continue;

                if (!proto.TryGetComponent<LatheComponent>(out var lathe, compFact))
                    continue;

                latheSys.AddRecipesFromPacks(latheTechs, lathe.DynamicPacks);

                if (proto.TryGetComponent<EmagLatheRecipesComponent>(out var emag, compFact))
                    latheSys.AddRecipesFromPacks(latheTechs, emag.EmagDynamicPacks);
            }

            Assert.Multiple(() =>
            {
                // check that every recipe a tech adds can be made on some lathe
                var unlockedTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
                foreach (var tech in protoManager.EnumeratePrototypes<TechnologyPrototype>())
                {
                    unlockedTechs.UnionWith(tech.RecipeUnlocks);
                    foreach (var recipe in tech.RecipeUnlocks)
                    {
                        Assert.That(latheTechs, Does.Contain(recipe), $"Recipe '{recipe}' from tech '{tech.ID}' cannot be unlocked on any lathes.");
                    }
                }

                // now check that every dynamic recipe a lathe lists can be unlocked
                foreach (var recipe in latheTechs)
                {
                    Assert.That(unlockedTechs, Does.Contain(recipe), $"Recipe '{recipe}' is dynamic on a lathe but cannot be unlocked by research.");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

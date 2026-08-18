#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Lathe;
using Content.Shared.Research.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

public sealed class ResearchTest : GameTest
{
    [SidedDependency(Side.Server)] private SharedLatheSystem _sLatheSystem = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public async Task DisciplineValidTierPrerequisitesTest()
    {
        var allTechs = SProtoMan.EnumeratePrototypes<TechnologyPrototype>().ToList();
        var disciplines = SProtoMan.EnumeratePrototypes<TechDisciplinePrototype>().ToDictionary(p => p.ID, p => p);

        using (Assert.EnterMultipleScope())
        {
            foreach (var tech in allTechs)
            {
                var discipline = disciplines[tech.Discipline];

                // Tier 1 techs don't have prerequisites
                if (tech.Tier == 1)
                    continue;

                Assert.That(tech.Tier, Is.GreaterThan(0), $"Technology {tech} has invalid tier {tech.Tier}.");
                Assert.That(discipline.TierPrerequisites.ContainsKey(tech.Tier),
                    $"Discipline {discipline.ID} does not have a {nameof(TechDisciplinePrototype.TierPrerequisites)} definition for tier {tech.Tier}");
            }
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public async Task AllTechPrintableTest()
    {
        var lathes = Pair.GetPrototypesWithComponent<LatheComponent>();
        var latheTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
        foreach (var (proto, latheComp) in lathes)
        {
            _sLatheSystem.AddRecipesFromPacks(latheTechs, latheComp.DynamicPacks);

            if (proto.TryComp<EmagLatheRecipesComponent>(out var emag, SEntMan.ComponentFactory))
                _sLatheSystem.AddRecipesFromPacks(latheTechs, emag.EmagDynamicPacks);
        }

        using (Assert.EnterMultipleScope())
        {
            // check that every recipe a tech adds can be made on some lathe
            var unlockedTechs = new HashSet<ProtoId<LatheRecipePrototype>>();
            foreach (var tech in SProtoMan.EnumeratePrototypes<TechnologyPrototype>())
            {
                unlockedTechs.UnionWith(tech.RecipeUnlocks);
                foreach (var recipe in tech.RecipeUnlocks)
                {
                    Assert.That(latheTechs, Does.Contain(recipe),
                        $"Recipe '{recipe}' from tech '{tech.ID}' cannot be unlocked on any lathes.");
                }
            }

            // now check that every dynamic recipe a lathe lists can be unlocked
            foreach (var recipe in latheTechs)
            {
                Assert.That(unlockedTechs, Does.Contain(recipe),
                    $"Recipe '{recipe}' is dynamic on a lathe but cannot be unlocked by research.");
            }
        }
    }
}

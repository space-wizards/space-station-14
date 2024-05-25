using Content.Shared.Objectives.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests;

/// <summary>
/// Makes sure that every entity with <c>ObjectivesComponent</c> has the <c>objectives</c> entityCategory,
/// in other words that every action inherits <c>BaseObjective</c>.
/// </summary>
/// <remarks>
/// Instead of copy pasting a test for every component/category pair this should be an attribute on RegisterComponent or something
/// </remarks>
[TestFixture]
public sealed class ObjectivesCategoryTest
{
    [ValidatePrototypeId<EntityCategoryPrototype>]
    public const string Objectives = "objectives";

    [Test]
    public async Task TestAllObjectivesInCategory()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false});
        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();

        await server.WaitPost(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
                {
                    if (!proto.TryGetComponent<ObjectiveComponent>(out _, factory))
                        continue;

                    Assert.That(proto.Categories, Does.Contain(Objectives), $"Objective prototype '{proto.ID}' is missing the objectives category, make it inherit BaseObjective");
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}

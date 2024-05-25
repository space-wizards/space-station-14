using Content.Shared.Actions.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.IntegrationTests.Tests.Actions;

/// <summary>
/// Makes sure that every entity with <c>ActionComponent</c> has the <c>actions</c> entityCategory,
/// in other words that every action inherits <c>BaseAction</c>.
/// </summary>
[TestFixture]
public sealed class ActionsCategoryTest
{
    [ValidatePrototypeId<EntityCategoryPrototype>]
    public const string Actions = "actions";

    [Test]
    public async Task TestAllActionsInCategory()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false});
        var server = pair.Server;
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var factory = server.ResolveDependency<IComponentFactory>();

        Assert.Multiple(() =>
        {
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<ActionComponent>(out _, factory))
                    continue;

                Assert.That(proto.Categories, Does.Contain(Actions), $"Action prototype '{proto.ID}' is missing the actions category, make it inherit BaseAction");
            }
        });

        await pair.CleanReturnAsync();
    }
}

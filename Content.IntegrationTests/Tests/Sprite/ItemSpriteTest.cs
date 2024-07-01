#nullable enable
using System.Collections.Generic;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Sprite;

/// <summary>
/// This test checks that all items have a visible sprite. The general rationale is that all items can be picked up
/// by players, thus they need to be visible and have a sprite that can be rendered on screen and in their hands GUI.
/// This has nothing to do with in-hand sprites.
/// </summary>
/// <remarks>
/// If a prototype fails this test, its probably either because it:
/// - Should be marked abstract
/// - inherits from BaseItem despite not being an item
/// - Shouldn't have an item component
/// - Is missing the required sprite information.
/// If none of the abveo are true, it might need to be added to the list of ignored components, see
/// <see cref="Ignored"/>
/// </remarks>
[TestFixture]
public sealed class PrototypeSaveTest
{
    private static readonly HashSet<string> Ignored = new()
    {
        // The only prototypes that should get ignored are those that REQUIRE setup to get a sprite. At that point it is
        // the responsibility of the spawner to ensure that a valid sprite is set.
        "VirtualItem"
    };

    [Test]
    public async Task AllItemsHaveSpritesTest()
    {
        var settings = new PoolSettings() { Connected = true }; // client needs to be in-game
        await using var pair = await PoolManager.GetServerClient(settings);
        List<EntityPrototype> badPrototypes = [];

        await pair.Client.WaitPost(() =>
        {
            foreach (var proto in pair.GetPrototypesWithComponent<ItemComponent>(Ignored))
            {
                var dummy = pair.Client.EntMan.Spawn(proto.ID);
                pair.Client.EntMan.RunMapInit(dummy, pair.Client.MetaData(dummy));
                var spriteComponent = pair.Client.EntMan.GetComponentOrNull<SpriteComponent>(dummy);
                if (spriteComponent?.Icon == null)
                    badPrototypes.Add(proto);
                pair.Client.EntMan.DeleteEntity(dummy);
            }
        });

        Assert.Multiple(() =>
        {
            foreach (var proto in badPrototypes)
            {
                Assert.Fail($"Item prototype has no sprite: {proto.ID}. It should probably either be marked as abstract, not be an item, or have a valid sprite");
            }
        });

        await pair.CleanReturnAsync();
    }
}

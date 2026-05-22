#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

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
public sealed class ItemSpriteTest : GameTest
{
    /// <summary>
    /// Prototypes listed here will be ignored by <see cref="AllItemsHaveSpritesTest"/>
    /// </summary>
    /// <remarks>
    /// The only prototypes that should get ignored are those that REQUIRE setup to get a sprite.
    /// At that point it is the responsibility of the spawner to ensure that a valid sprite is set.
    /// </remarks>
    private static readonly HashSet<string> Ignored =
    [
        "VirtualItem"
    ];

    [Test]
    [RunOnSide(Side.Client)]
    [Description("Checks that all items have a visible sprite.")]
    public async Task AllItemsHaveSpritesTest()
    {
        foreach (var (proto, _) in Pair.GetPrototypesWithComponent<ItemComponent>(ignored: Ignored))
        {
            var dummy = CSpawn(proto.ID);
            CEntMan.RunMapInit(dummy, Client.MetaData(dummy));
            var spriteComponent = CEntMan.GetComponentOrNull<SpriteComponent>(dummy);

            Assert.That(spriteComponent?.Icon, Is.Not.Null, $"Item prototype \"{proto.ID}\" has no sprite. It should probably either be marked as abstract, not be an item, or have a valid sprite");

            CDeleteNow(dummy);
        }
    }
}

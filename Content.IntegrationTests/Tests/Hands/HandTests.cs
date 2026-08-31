#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Hands;

public sealed class HandTests : GameTest
{
    private const string TestPickUpThenDropInContainerTestBox = "TestPickUpThenDropInContainerTestBox";
    private static readonly EntProtoId Crowbar = "Crowbar";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {TestPickUpThenDropInContainerTestBox}
  name: box
  components:
  - type: EntityStorage
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container
";

    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        DummyTicker = false
    };

    [SidedDependency(Side.Server)] private SharedContainerSystem _sContainerSystem = default!;
    [SidedDependency(Side.Server)] private EntityStorageSystem _sEntityStorageSystem = default!;
    [SidedDependency(Side.Server)] private SharedHandsSystem _sHandsSystem = default!;
    [SidedDependency(Side.Server)] private TransformSystem _sTransformSystem = default!;

    [Test]
    public async Task TestPickupDrop()
    {
        await Pair.CreateTestMap();

        EntityUid item = default;
        EntityUid player = default;
        HandsComponent hands = default!;
        await Server.WaitPost(() =>
        {
            player = ServerSession!.AttachedEntity!.Value;
            var xform = SComp<TransformComponent>(player);
            item = SSpawnAtPosition(Crowbar, xform.Coordinates);
            hands = SComp<HandsComponent>(player);
            _sHandsSystem.TryPickup(player, item, hands.ActiveHandId!);
        });

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await RunTicksSync(5);
        Assert.That(_sHandsSystem.GetActiveItem((player, hands)), Is.EqualTo(item));

        await Server.WaitPost(() =>
        {
            _sHandsSystem.TryDrop(player, item);
        });

        await RunTicksSync(5);
        Assert.That(_sHandsSystem.GetActiveItem((player, hands)), Is.Null);
    }

    [Test]
    public async Task TestPickUpThenDropInContainer()
    {
        await Pair.CreateTestMap();
        Assert.That(TestMap, Is.Not.Null);

        EntityUid item = default;
        EntityUid box = default;
        EntityUid player = default;
        HandsComponent hands = default!;

        // spawn the elusive box and crowbar at the coordinates
        await Server.WaitPost(() => box = SSpawnAtPosition(TestPickUpThenDropInContainerTestBox, TestMap.GridCoords));
        await Server.WaitPost(() => item = SSpawnAtPosition(Crowbar, TestMap.GridCoords));
        // place the player at the exact same coordinates and have them grab the crowbar
        await Server.WaitPost(() =>
        {
            player = ServerSession!.AttachedEntity!.Value;
            _sTransformSystem.PlaceNextTo(player, item);
            hands = SComp<HandsComponent>(player);
            _sHandsSystem.TryPickup(player, item, hands.ActiveHandId!);
        });

        await RunTicksSync(5);
        Assert.That(_sHandsSystem.GetActiveItem((player, hands)), Is.EqualTo(item));

        // Open then close the box to place the player, who is holding the crowbar, inside of it
        await Server.WaitPost(() =>
        {
            _sEntityStorageSystem.OpenStorage(box);
            _sEntityStorageSystem.CloseStorage(box);
        });
        await RunTicksSync(5);
        Assert.That(_sContainerSystem.IsEntityInContainer(player), Is.True);

        // Dropping the item while the player is inside the box should cause the item
        // to also be inside the same container the player is in now,
        // with the item not being in the player's hands
        await Server.WaitPost(() =>
        {
            _sHandsSystem.TryDrop(player, item);
        });

        await RunTicksSync(5);
        var xform = SComp<TransformComponent>(player);
        var itemXform = SComp<TransformComponent>(item);
        Assert.That(_sHandsSystem.GetActiveItem((player, hands)), Is.Not.EqualTo(item));
        Assert.That(_sContainerSystem.IsInSameOrNoContainer((player, xform), (item, itemXform)));
    }
}

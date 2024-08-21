using System.Linq;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Hands;

[TestFixture]
public sealed class HandTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: TestPickUpThenDropInContainerTestBox
  name: box
  components:
  - type: EntityStorage
  - type: ContainerContainer
    containers:
      entity_storage: !type:Container
";


    [Test]
    public async Task TestPickupDrop()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var server = pair.Server;

        var entMan = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var sys = entMan.System<SharedHandsSystem>();
        var tSys = entMan.System<TransformSystem>();

        var data = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        EntityUid item = default;
        EntityUid player = default;
        HandsComponent hands = default!;
        await server.WaitPost(() =>
        {
            player = playerMan.Sessions.First().AttachedEntity!.Value;
            var xform = entMan.GetComponent<TransformComponent>(player);
            item = entMan.SpawnEntity("Crowbar", tSys.GetMapCoordinates(player, xform: xform));
            hands = entMan.GetComponent<HandsComponent>(player);
            sys.TryPickup(player, item, hands.ActiveHand!);
        });

        // run ticks here is important, as errors may happen within the container system's frame update methods.
        await pair.RunTicksSync(5);
        Assert.That(hands.ActiveHandEntity, Is.EqualTo(item));

        await server.WaitPost(() =>
        {
            sys.TryDrop(player, item, null!);
        });

        await pair.RunTicksSync(5);
        Assert.That(hands.ActiveHandEntity, Is.Null);

        await server.WaitPost(() => mapMan.DeleteMap(data.MapId));
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task TestPickUpThenDropInContainer()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await pair.RunTicksSync(5);

        var entMan = server.ResolveDependency<IEntityManager>();
        var playerMan = server.ResolveDependency<IPlayerManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var sys = entMan.System<SharedHandsSystem>();
        var tSys = entMan.System<TransformSystem>();
        var containerSystem = server.System<SharedContainerSystem>();

        EntityUid item = default;
        EntityUid box = default;
        EntityUid player = default;
        HandsComponent hands = default!;

        // spawn the elusive box and crowbar at the coordinates
        await server.WaitPost(() => box = server.EntMan.SpawnEntity("TestPickUpThenDropInContainerTestBox", map.GridCoords));
        await server.WaitPost(() => item = server.EntMan.SpawnEntity("Crowbar", map.GridCoords));
        // place the player at the exact same coordinates and have them grab the crowbar
        await server.WaitPost(() =>
        {
            player = playerMan.Sessions.First().AttachedEntity!.Value;
            tSys.PlaceNextTo(player, item);
            hands = entMan.GetComponent<HandsComponent>(player);
            sys.TryPickup(player, item, hands.ActiveHand!);
        });
        await pair.RunTicksSync(5);
        Assert.That(hands.ActiveHandEntity, Is.EqualTo(item));

        // Open then close the box to place the player, who is holding the crowbar, inside of it
        var storage = server.System<EntityStorageSystem>();
        await server.WaitPost(() =>
        {
            storage.OpenStorage(box);
            storage.CloseStorage(box);
        });
        await pair.RunTicksSync(5);
        Assert.That(containerSystem.IsEntityInContainer(player), Is.True);

        // Dropping the item while the player is inside the box should cause the item
        // to also be inside the same container the player is in now,
        // with the item not being in the player's hands
        await server.WaitPost(() =>
        {
            sys.TryDrop(player, item, null!);
        });
        await pair.RunTicksSync(5);
        var xform = entMan.GetComponent<TransformComponent>(player);
        var itemXform = entMan.GetComponent<TransformComponent>(item);
        Assert.That(hands.ActiveHandEntity, Is.Not.EqualTo(item));
        Assert.That(containerSystem.IsInSameOrNoContainer((player, xform), (item, itemXform)));

        await server.WaitPost(() => mapMan.DeleteMap(map.MapId));
        await pair.CleanReturnAsync();
    }
}

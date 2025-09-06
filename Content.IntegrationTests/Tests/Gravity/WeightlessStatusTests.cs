using System.Linq;
using System.Numerics;
using Content.Server.Gravity;
using Robust.Server.Player;
using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Gravity;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.UnitTesting;
using Content.Shared.Item.ItemToggle;

namespace Content.IntegrationTests.Tests.Gravity;

/// <summary>
/// These tests check weightlessness.
/// </summary>
[TestOf(typeof(SharedGravitySystem))]
[TestOf(typeof(GravityGeneratorComponent))]
public sealed class WeightlessStatusTests
{
    // Magboots, which make you non-weightless when on a grid, equipped and toggled on
    private static readonly EntProtoId MagBootsProto = "ClothingShoesBootsMag";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: HumanWeightlessDummy
  id: HumanWeightlessDummy
  components:
  # We need a sprite so that the animation can play.
  # Surely we will never delete this one!
  - type: Sprite
    sprite: Objects/Fun/Plushies/lizard.rsi
    state: icon
  - type: Alerts
  - type: Physics
    bodyType: Dynamic
  - type: GravityAffected
  - type: Inventory
  - type: InventorySlots
  - type: ContainerContainer
  - type: AnimationPlayer
  - type: FloatingVisuals
    animationTime: 0.2 # shorter so we don't have to simulate that long

- type: entity
  name: WeightlessGravityGeneratorDummy
  id: WeightlessGravityGeneratorDummy
  components:
  - type: GravityGenerator
  - type: PowerCharge
    windowTitle: gravity-generator-window-title
    idlePower: 50
    chargeRate: 1000000000 # Set this really high so it discharges in a single tick.
    activePower: 500
  - type: ApcPowerReceiver
    needsPower: false
  - type: UserInterface
";

    private void AssertServer(RobustIntegrationTest.ServerIntegrationInstance server, EntityUid target, bool weightless)
    {
        var weightlessAlert = SharedGravitySystem.WeightlessAlert;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var gravity = server.System<SharedGravitySystem>();
        var alerts = server.System<AlertsSystem>();

        Assert.Multiple(() =>
        {
            Assert.That(gravity.IsWeightless(target), Is.EqualTo(weightless));
            Assert.That(entityManager.TryGetComponent(target, out AlertsComponent alertsComp));
            Assert.That(alerts.IsShowingAlert(target, weightlessAlert), Is.EqualTo(weightless));
        });
    }

    private void AssertClient(RobustIntegrationTest.ClientIntegrationInstance client, EntityUid target, bool weightless)
    {
        var weightlessAlert = SharedGravitySystem.WeightlessAlert;
        var entityManager = client.ResolveDependency<IEntityManager>();
        var gravity = client.System<SharedGravitySystem>();
        var animation = client.System<AnimationPlayerSystem>();

        Assert.Multiple(() =>
        {
            Assert.That(gravity.IsWeightless(target), Is.EqualTo(weightless));
            Assert.That(animation.HasRunningAnimation(target, FloatingVisualsComponent.AnimationKey), Is.EqualTo(weightless));
        });
    }

    /// <summary>
    /// Tests that a gravity generator makes mobs on a grid non-weightless.
    /// Also tests if event-based weightlessness chaches the correct values
    /// and works with PVS and reconnects.
    /// </summary>
    [Test]
    public async Task GravityGeneratorTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var client = pair.Client;
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var transformSystem = server.System<SharedTransformSystem>();

        var testMap = await pair.CreateTestMap();
        var sSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sObserver = sSession.AttachedEntity!.Value;

        var pos1 = new EntityCoordinates(testMap.Grid.Owner, new Vector2(0.5f, 0.5f)); // on the grid
        var pos2 = new EntityCoordinates(testMap.MapUid, new Vector2(0, 5)); // off-grid
        var pos3 = new EntityCoordinates(testMap.MapUid, new Vector2(0, 1000)); // outside PVS range

        // spawn one dummy on the grid and another one off-grid
        EntityUid sHuman1 = default;
        EntityUid sHuman2 = default;
        await server.WaitPost(() =>
        {
            sHuman1 = entityManager.SpawnEntity("HumanWeightlessDummy", pos1);
            sHuman2 = entityManager.SpawnEntity("HumanWeightlessDummy", pos2);
        });

        // check that they spawned correctly
        Assert.Multiple(() =>
        {
            Assert.That(entityManager.GetComponent<TransformComponent>(sHuman1).ParentUid, Is.EqualTo(testMap.Grid.Owner));
            Assert.That(entityManager.GetComponent<TransformComponent>(sHuman2).ParentUid, Is.EqualTo(testMap.MapUid));
        });

        // Let WeightlessSystem and GravitySystem tick
        await pair.RunSeconds(0.5f);
        var cHuman1 = pair.ToClientUid(sHuman1);
        var cHuman2 = pair.ToClientUid(sHuman2);

        // Check that both dummies are weightless
        AssertServer(pair.Server, sHuman1, true);
        AssertClient(pair.Client, cHuman1, true);
        AssertServer(pair.Server, sHuman2, true);
        AssertClient(pair.Client, cHuman2, true);

        // Spawn a gravity generator
        var sGenerator = EntityUid.Invalid;
        await server.WaitPost(() =>
        {
            sGenerator = entityManager.SpawnEntity("WeightlessGravityGeneratorDummy", pos1);
        });

        await pair.RunSeconds(0.5f);

        // Check that the dummy on the grid is no longer weightless
        AssertServer(pair.Server, sHuman1, false);
        AssertClient(pair.Client, cHuman1, false);
        AssertServer(pair.Server, sHuman2, true);
        AssertClient(pair.Client, cHuman2, true);

        // Swap positions
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sHuman1, pos2);
            transformSystem.SetCoordinates(sHuman2, pos1);
        });

        await pair.RunSeconds(0.5f);

        // Weightlessness should be swapped now
        AssertServer(pair.Server, sHuman1, true);
        AssertClient(pair.Client, cHuman1, true);
        AssertServer(pair.Server, sHuman2, false);
        AssertClient(pair.Client, cHuman2, false);

        // Move the client far away so that the entities leave PVS range
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos3);
        });

        await pair.RunSeconds(0.5f);

        // Move the client back
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos1);
        });

        // Only simulate very short to make sure the floating animation is not running when reentering PVS range
        await pair.RunTicksSync(3);

        // Check if everything is still correct
        AssertServer(pair.Server, sHuman1, true);
        AssertClient(pair.Client, cHuman1, true);
        AssertServer(pair.Server, sHuman2, false);
        AssertClient(pair.Client, cHuman2, false);

        // Move the client far away again
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos3);
        });

        await pair.RunSeconds(0.5f);

        // Swap positions again, this time while outside PVS range
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sHuman1, pos1);
            transformSystem.SetCoordinates(sHuman2, pos2);
        });

        await pair.RunSeconds(0.5f);

        // Move the client back again
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos1);
        });

        await pair.RunTicksSync(3);

        // Check after moving back
        AssertServer(pair.Server, sHuman1, false);
        AssertClient(pair.Client, cHuman1, false);
        AssertServer(pair.Server, sHuman2, true);
        AssertClient(pair.Client, cHuman2, true);

        // reconnect the client to check if the cached weightlessness refreshes correctly
        await pair.Reconnect();

        await pair.RunSeconds(0.5f);

        // Check again
        AssertServer(pair.Server, sHuman1, false);
        AssertClient(pair.Client, cHuman1, false);
        AssertServer(pair.Server, sHuman2, true);
        AssertClient(pair.Client, cHuman2, true);

        // Delete the gravity generator
        await server.WaitPost(() =>
        {
            entityManager.DeleteEntity(sGenerator);
        });

        // Both should be weightless
        AssertServer(pair.Server, sHuman1, true);
        AssertClient(pair.Client, cHuman1, true);
        AssertServer(pair.Server, sHuman2, true);
        AssertClient(pair.Client, cHuman2, true);

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Tests that magboots make a mob non-weightless when on a grid and toggled on.
    /// Also tests if event-based weightlessness chaches the correct values
    /// and works with PVS and reconnects.
    /// </summary>
    /// <remarks>
    /// TODO: The same test for diona rooting.
    /// </remarks>
    [Test]
    public async Task MagBootsTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true, DummyTicker = false });
        var client = pair.Client;
        var server = pair.Server;

        var entityManager = server.ResolveDependency<IEntityManager>();
        var transformSystem = server.System<SharedTransformSystem>();

        var testMap = await pair.CreateTestMap();
        var sSession = server.ResolveDependency<IPlayerManager>().Sessions.Single();
        var sObserver = sSession.AttachedEntity!.Value;

        var pos1 = new EntityCoordinates(testMap.Grid.Owner, new Vector2(0.5f, 0.5f)); // on the grid
        var pos2 = new EntityCoordinates(testMap.MapUid, new Vector2(0, 5)); // off-grid
        var pos3 = new EntityCoordinates(testMap.MapUid, new Vector2(0, 1000)); // outside PVS range

        // spawn a dummy on the grid
        EntityUid sHuman = default;
        await server.WaitPost(() =>
        {
            sHuman = entityManager.SpawnEntity("HumanWeightlessDummy", pos1);
        });

        // check that they spawned correctly
        Assert.Multiple(() =>
        {
            Assert.That(entityManager.GetComponent<TransformComponent>(sHuman).ParentUid, Is.EqualTo(testMap.Grid.Owner));
        });

        // Let WeightlessSystem and GravitySystem tick
        await pair.RunSeconds(0.5f);
        var cHuman = pair.ToClientUid(sHuman);

        // Check that the dummy is weightless
        AssertServer(pair.Server, sHuman, true);
        AssertClient(pair.Client, cHuman, true);

        // spawn a pair of magboots and let the dummy equip them
        EntityUid sBoots = default;
        var invSystem = entityManager.System<InventorySystem>();
        await server.WaitAssertion(() =>
        {
            sBoots = entityManager.SpawnEntity(MagBootsProto, pos1);
            Assert.That(invSystem.TryEquip(sHuman, sBoots, "shoes"));
        });

        await pair.RunSeconds(0.5f);

        // The boots are still toggled off, we should be weightless
        AssertServer(pair.Server, sHuman, true);
        AssertClient(pair.Client, cHuman, true);

        var itemToggleSystem = entityManager.System<ItemToggleSystem>();
        // Toggle the boots on
        await server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(sBoots, true, predicted: false));
        });

        await pair.RunSeconds(0.5f);

        // The boots are toggled on, we should not be weightless
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        // Unequip the boots while they are still toggled on
        await server.WaitAssertion(() =>
        {
            Assert.That(invSystem.TryUnequip(sHuman, "shoes"));
        });

        await pair.RunSeconds(0.5f);

        // Not wearing boots, we should be weightless
        AssertServer(pair.Server, sHuman, true);
        AssertClient(pair.Client, cHuman, true);

        // Equip the boots again while they are still toggled on
        await server.WaitAssertion(() =>
        {
            Assert.That(invSystem.TryEquip(sHuman, sBoots, "shoes"));
        });

        await pair.RunSeconds(0.5f);

        // We are wearing the boots again, we should not be weightless
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        // Teleport the dummy off-grid
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sHuman, pos2);
        });

        await pair.RunSeconds(0.5f);

        // We are in space, we should be weightless
        AssertServer(pair.Server, sHuman, true);
        AssertClient(pair.Client, cHuman, true);

        // Teleport the dummy back to the grid
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sHuman, pos1);
        });

        await pair.RunSeconds(0.5f);

        // We are on the grid again and the boots still active, we should not be weightless
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        // Teleport the observer out of PVS range and back
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos3);
        });

        await pair.RunSeconds(0.5f);

        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos1);
        });

        // Only simulate very short to make sure the floating animation is not running when reentering PVS range
        await pair.RunTicksSync(3);

        // Check again
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        // Teleport the observer out of PVS range
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos3);
        });

        await pair.RunSeconds(0.5f);

        // Toggle the boots off while out of PVS range
        await server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(sBoots, false, predicted: false));
        });

        await pair.RunSeconds(0.5f);

        // Teleport the observer back to the grid
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos1);
        });

        await pair.RunTicksSync(3);

        // The boots are off, we should be weightless
        AssertServer(pair.Server, sHuman, true);
        AssertClient(pair.Client, cHuman, true);

        // Repeat the last steps, but toggle the boots on again
        // Teleport the observer out of PVS range
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos3);
        });

        await pair.RunSeconds(0.5f);

        // Toggle the boots on while out of PVS range
        await server.WaitAssertion(() =>
        {
            Assert.That(itemToggleSystem.TrySetActive(sBoots, true, predicted: false));
        });

        await pair.RunSeconds(0.5f);

        // Teleport the observer back to the grid
        await server.WaitPost(() =>
        {
            transformSystem.SetCoordinates(sObserver, pos1);
        });

        await pair.RunTicksSync(3);

        // The boots are on, we should not be weightless
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        // reconnect the client to check if the cached weightlessness refreshes correctly
        await pair.Reconnect();

        await pair.RunSeconds(0.5f);

        // Check again
        AssertServer(pair.Server, sHuman, false);
        AssertClient(pair.Client, cHuman, false);

        await pair.CleanReturnAsync();
    }
}

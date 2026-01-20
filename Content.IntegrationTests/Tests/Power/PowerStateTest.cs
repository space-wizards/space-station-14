using Content.Shared.Coordinates;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture]
public sealed class PowerStateTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: PowerStateApcReceiverDummy
  components:
  - type: ApcPowerReceiver
  - type: ExtensionCableReceiver
  - type: Transform
    anchored: true
  - type: PowerState
    isWorking: false
    idlePowerDraw: 10
    workingPowerDraw: 50
";

    /// <summary>
    /// Asserts that switching from idle to working updates the power receiver load to the working draw.
    /// </summary>
    [Test]
    public async Task SetWorkingState_IdleToWorking_UpdatesLoad()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entManager.System<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);

            mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

            var ent = entManager.SpawnEntity("PowerStateApcReceiverDummy", grid.Owner.ToCoordinates());

            var receiver = entManager.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
            var powerState = entManager.GetComponent<PowerStateComponent>(ent);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            });

            var system = entManager.System<PowerStateSystem>();
            system.SetWorkingState((ent, powerState), true);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.True);
                Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Asserts that switching from working to idle updates the power receiver load to the idle draw.
    /// </summary>
    [Test]
    public async Task SetWorkingState_WorkingToIdle_UpdatesLoad()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entManager.System<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);

            mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

            var ent = entManager.SpawnEntity("PowerStateApcReceiverDummy", grid.Owner.ToCoordinates());

            var receiver = entManager.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
            var powerState = entManager.GetComponent<PowerStateComponent>(ent);
            var system = entManager.System<PowerStateSystem>();
            Entity<PowerStateComponent> newEnt = (ent, powerState);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            });

            system.SetWorkingState(newEnt, true);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.True);
                Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
            });

            system.SetWorkingState(newEnt, false);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Asserts that setting the working state to the current state does not change the power receiver load.
    /// </summary>
    [Test]
    public async Task SetWorkingState_AlreadyInState_NoChange()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;

        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var mapSys = entManager.System<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            mapSys.CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);

            mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

            var ent = entManager.SpawnEntity("PowerStateApcReceiverDummy", grid.Owner.ToCoordinates());

            var receiver = entManager.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
            var powerState = entManager.GetComponent<PowerStateComponent>(ent);
            var system = entManager.System<PowerStateSystem>();
            Entity<PowerStateComponent> valueTuple = (ent, powerState);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            });

            system.SetWorkingState(valueTuple, false);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            });

            system.SetWorkingState(valueTuple, true);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.True);
                Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
            });

            system.SetWorkingState(valueTuple, true);

            Assert.Multiple(() =>
            {
                Assert.That(powerState.IsWorking, Is.True);
                Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
            });
        });

        await pair.CleanReturnAsync();
    }
}


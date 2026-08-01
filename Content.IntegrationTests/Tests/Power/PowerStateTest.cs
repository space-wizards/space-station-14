using System.Collections.Generic;
using Content.Shared.Coordinates;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Server.Power.Pow3r; // DS14
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting; // DS14

namespace Content.IntegrationTests.Tests.Power;

[TestFixture]
public sealed class PowerStateTest
{
    // DS14-start
    [Test]
    public void GenIdStorageReportsHistoricalCapacity()
    {
        var storage = new PowerState.GenIdStorage<object>();

        Assert.Multiple(() =>
        {
            Assert.That(storage.Count, Is.Zero);
            Assert.That(storage.Capacity, Is.Zero);
        });

        storage.Allocate(out var id) = new object();
        var allocatedCapacity = storage.Capacity;

        Assert.Multiple(() =>
        {
            Assert.That(storage.Count, Is.EqualTo(1));
            Assert.That(allocatedCapacity, Is.GreaterThanOrEqualTo(1));
        });

        storage.Free(id);

        Assert.Multiple(() =>
        {
            Assert.That(storage.Count, Is.Zero);
            Assert.That(storage.Capacity, Is.EqualTo(allocatedCapacity));
        });
    }

    [Test]
    public void ChargeBoundaryClampsAndQueuesStorageOnce()
    {
        const float capacity = 0.0012996652f;
        const float initialStorage = 0.00022773328f;
        const float efficiency = 0.39115587f;
        const float frameTime = 0.24549939f;

        var state = new PowerState();
        var network = new PowerState.Network();
        state.Networks.Allocate(out network.Id) = network;

        var supply = new PowerState.Supply
        {
            MaxSupply = 1f,
            SupplyRampTolerance = 1f,
            LinkedNetwork = network.Id,
        };
        state.Supplies.Allocate(out supply.Id) = supply;
        state.AttachSupply(supply);
        network.Supplies.Add(supply.Id);

        var battery = new PowerState.Battery
        {
            Capacity = capacity,
            CurrentStorage = initialStorage,
            Efficiency = efficiency,
            MaxChargeRate = 1f,
            LinkedNetworkCharging = network.Id,
        };
        state.Batteries.Allocate(out battery.Id) = battery;
        state.AttachBattery(battery);
        network.BatteryLoads.Add(battery.Id);
        DrainBatteryStorageChanges(state);

        new BatteryRampPegSolver(disableParallel: true)
            .Tick(frameTime, state, new TestingParallelManager());

        var changes = DrainBatteryStorageChanges(state);
        Assert.Multiple(() =>
        {
            Assert.That(battery.CurrentStorage, Is.EqualTo(capacity));
            Assert.That(changes, Is.EqualTo(new[] { battery.Id }));
        });
    }

    [Test]
    public void DischargeBoundaryClampsAndQueuesStorageOnce()
    {
        const float capacity = 100f;
        const float initialStorage = 0.20860587f;
        const float frameTime = 0.009083092f;

        var state = new PowerState();
        var network = new PowerState.Network();
        state.Networks.Allocate(out network.Id) = network;

        var load = new PowerState.Load
        {
            DesiredPower = 1_000f,
            LinkedNetwork = network.Id,
        };
        state.Loads.Allocate(out load.Id) = load;
        state.AttachLoad(load);
        network.Loads.Add(load.Id);

        var battery = new PowerState.Battery
        {
            Capacity = capacity,
            CurrentStorage = initialStorage,
            MaxSupply = 1_000f,
            SupplyRampTolerance = 1_000f,
            LinkedNetworkDischarging = network.Id,
        };
        state.Batteries.Allocate(out battery.Id) = battery;
        state.AttachBattery(battery);
        network.BatterySupplies.Add(battery.Id);
        DrainBatteryStorageChanges(state);

        new BatteryRampPegSolver(disableParallel: true)
            .Tick(frameTime, state, new TestingParallelManager());

        var changes = DrainBatteryStorageChanges(state);
        Assert.Multiple(() =>
        {
            Assert.That(battery.CurrentStorage, Is.Zero);
            Assert.That(changes, Is.EqualTo(new[] { battery.Id }));
        });
    }

    private static List<PowerState.NodeId> DrainBatteryStorageChanges(PowerState state)
    {
        var changes = new List<PowerState.NodeId>();
        while (state.TryDequeueChangedBatteryStorage(out var id))
        {
            changes.Add(id);
        }

        return changes;
    }
    // DS14-end

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

            var system = entManager.System<SharedPowerStateSystem>();
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
            var system = entManager.System<SharedPowerStateSystem>();
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
            var system = entManager.System<SharedPowerStateSystem>();
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


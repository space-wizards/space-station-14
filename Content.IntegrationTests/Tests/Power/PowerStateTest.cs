using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Helpers;
using Content.Server.Power.Components;
using Content.Shared.Coordinates;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.Tests.Power;

[TestFixture]
public sealed class PowerStateTest : GameTest
{
    private const string ProtoId = "PowerStateApcReceiverDummy";
    private const string DebugApc = "ApcDummy2";

    private static readonly EntProtoId LvCable = "CableApcExtension";

    [TestPrototypes]
    private const string Prototypes =
        $"""
        - type: entity
          id: {ProtoId}
          components:
          - type: ApcPowerReceiver
          - type: ExtensionCableReceiver
          - type: Transform
            anchored: true
          - type: PowerState
            isWorking: false
            idlePowerDraw: 10
            workingPowerDraw: 50
        
        - type: entity
          id: {DebugApc}
          components:
          - type: Battery
            netsync: false
            maxCharge: 10000
            startingCharge: 10000
          - type: PowerNetworkBattery
            maxChargeRate: 1000
            maxSupply: 1000
            supplyRampTolerance: 1000
          - type: BatteryCharger
            voltage: Medium
          - type: BatteryDischarger
            voltage: Apc
          - type: Apc
            voltage: Apc
          - type: NodeContainer
            nodes:
              input:
                !type:CableDeviceNode
                nodeGroupID: MVPower
              output:
                !type:CableDeviceNode
                nodeGroupID: Apc
          - type: Transform
            anchored: true
          - type: UserInterface
            interfaces:
              enum.ApcUiKey.Key:
                type: ApcBoundUserInterface
          - type: AccessReader
            access: [['Engineering']]
        """;

    [SidedDependency(Side.Server)] private SharedMapSystem _mapSys = default!;
    [SidedDependency(Side.Server)] private PowerStateSystem _powerStateSys = default!;
    [SidedDependency(Side.Server)] private OperatingChangedListenerSystem _operatingChangedListener = default!;
    [SidedDependency(Side.Server)] private TurnedOnChangedListenerSystem _turnedOnChangedListener = default!;

    /// <summary>
    /// Asserts that switching from idle to working updates the power receiver load to the working draw.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void SetWorkingState_IdleToWorking_UpdatesLoad()
    {
        _mapSys.CreateMap(out var mapId);
        var grid = _mapSys.CreateGridEntity(mapId);

        _mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

        var ent = SSpawnAtPosition(ProtoId, grid.Owner.ToCoordinates());
        SEntMan.EnsureComponent<TestListenerComponent>(ent);

        var receiver = SEntMan.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
        var powerState = SEntMan.GetComponent<PowerStateComponent>(ent);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.False);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState((ent, powerState), true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.True);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
        }

        var turnedOnChangedEvents = _turnedOnChangedListener.GetEvents(ent)
                                                          .ToArray();
        Assert.That(turnedOnChangedEvents, Has.Exactly(1).Items, $"Expected to have only 1 {nameof(TurnedOnStateChanged)}, but got {turnedOnChangedEvents.Length}.");
        Assert.That(turnedOnChangedEvents[0].IsTurnedOn, Is.True);

        var operatingChangedEvents = _operatingChangedListener.GetEvents(ent)
                                                          .ToArray();
        Assert.That(operatingChangedEvents, Is.Empty, "Entity is unpowered so IsPowered was not changed.");
    }

    /// <summary>
    /// Asserts that switching from working to idle updates the power receiver load to the idle draw.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void SetWorkingState_WorkingToIdle_UpdatesLoad()
    {
        _mapSys.CreateMap(out var mapId);
        var grid = _mapSys.CreateGridEntity(mapId);

        _mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

        var ent = SSpawnAtPosition(ProtoId, grid.Owner.ToCoordinates());
        SEntMan.EnsureComponent<TestListenerComponent>(ent);

        var receiver = SEntMan.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
        var powerState = SEntMan.GetComponent<PowerStateComponent>(ent);
        Entity<PowerStateComponent> newEnt = (ent, powerState);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.False);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState(newEnt, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.True);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState(newEnt, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.False);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
        }

        var turnedOnChangedEvents = _turnedOnChangedListener.GetEvents(ent)
                                                            .ToArray();
        Assert.That(turnedOnChangedEvents, Has.Exactly(2).Items, $"Expected to have only 1 {nameof(TurnedOnStateChanged)}, but got {turnedOnChangedEvents.Length}.");
        Assert.That(turnedOnChangedEvents[0].IsTurnedOn, Is.True);
        Assert.That(turnedOnChangedEvents[1].IsTurnedOn, Is.False);

        var operatingChangedEvents = _operatingChangedListener.GetEvents(ent)
                                                              .ToArray();
        Assert.That(operatingChangedEvents, Is.Empty, "Entity is unpowered so IsPowered was not changed.");
    }

    /// <summary>
    /// Asserts that setting the working state to the current state does not change the power receiver load.
    /// </summary>
    [Test]
    [RunOnSide(Side.Server)]
    public void SetWorkingState_AlreadyInState_NoChange()
    {
        _mapSys.CreateMap(out var mapId);
        var grid = _mapSys.CreateGridEntity(mapId);

        _mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

        var ent = SSpawnAtPosition(ProtoId, grid.Owner.ToCoordinates());
        SEntMan.EnsureComponent<TestListenerComponent>(ent);

        var receiver = SEntMan.GetComponent<Server.Power.Components.ApcPowerReceiverComponent>(ent);
        var powerState = SEntMan.GetComponent<PowerStateComponent>(ent);
        Entity<PowerStateComponent> valueTuple = (ent, powerState);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.False);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState(valueTuple, false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.False);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState(valueTuple, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.True);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
        }

        _powerStateSys.SetWorkingState(valueTuple, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(powerState.IsTurnedOn, Is.True);
            Assert.That(powerState.IsOperating, Is.False);
            Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
        }

        var turnedOnChangedEvents = _turnedOnChangedListener.GetEvents(ent)
                                                            .ToArray();
        Assert.That(turnedOnChangedEvents, Has.Exactly(1).Items, $"Expected to have only 1 {nameof(TurnedOnStateChanged)}, but got {turnedOnChangedEvents.Length}.");
        var operatingChangedEvents = _operatingChangedListener.GetEvents(ent)
                                                              .ToArray();
        Assert.That(operatingChangedEvents, Is.Empty, "Entity is unpowered so IsPowered was not changed.");
    }

    /// <summary>
    /// Asserts that setting the working state with running power fires IsPowered change event.
    /// </summary>
    [Test]
    public async Task SetWorkingState_HavePowerSource_IsPoweredChanged()
    {
        EntityUid ent = default;
        await Server.WaitPost(() =>
        {
            _mapSys.CreateMap(out var mapId);
            var grid = _mapSys.CreateGridEntity(mapId);

            _mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

            var apc = SEntMan.SpawnAttachedTo(DebugApc, grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnAttachedTo(LvCable, grid.Owner.ToCoordinates(0, 0));

            ent = SSpawnAtPosition(ProtoId, grid.Owner.ToCoordinates());
            SEntMan.EnsureComponent<TestListenerComponent>(ent);
        });

        // wait for power to set up
        await Server.WaitRunTicks(1);

        var receiver = SEntMan.GetComponent<ApcPowerReceiverComponent>(ent);
        var powerState = SEntMan.GetComponent<PowerStateComponent>(ent);

        await Server.WaitAssertion(() =>
        {
            _powerStateSys.SetWorkingState((ent, powerState), true);
        });

        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(powerState.IsTurnedOn, Is.True);
                Assert.That(powerState.IsOperating, Is.True);
                Assert.That(receiver.Load, Is.EqualTo(powerState.WorkingPowerDraw).Within(0.01f));
            }

            var turnedOnChangedEvents = _turnedOnChangedListener.GetEvents(ent)
                                                              .ToArray();
            Assert.That(turnedOnChangedEvents,
                Has.Exactly(1).Items,
                $"Expected to have only 1 {nameof(TurnedOnStateChanged)}, but got {turnedOnChangedEvents.Length}.");
            var operatingChangedEvents = _operatingChangedListener.GetEvents(ent)
                                                              .ToArray();
            Assert.That(operatingChangedEvents, Has.Exactly(1).Items, "Entity is unpowered so IsPowered was not changed.");
            Assert.That(operatingChangedEvents[0].IsOperating, Is.True);
        });
    }


    /// <summary>
    /// Asserts that setting the working state with running power fires IsPowered change event.
    /// </summary>
    [Test]
    public async Task SetWorkingState_HavePowerSourceTurnOnTurnOff_IsPoweredChangedEventWithFalse()
    {
        EntityUid ent = default;
        await Server.WaitPost(() =>
        {
            _mapSys.CreateMap(out var mapId);
            var grid = _mapSys.CreateGridEntity(mapId);

            _mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));

            var apc = SEntMan.SpawnAttachedTo(DebugApc, grid.Owner.ToCoordinates(0, 0));
            SEntMan.SpawnAttachedTo(LvCable, grid.Owner.ToCoordinates(0, 0));

            ent = SSpawnAtPosition(ProtoId, grid.Owner.ToCoordinates());
            SEntMan.EnsureComponent<TestListenerComponent>(ent);
        });

        // wait for power to set up
        await Server.WaitRunTicks(1);

        var receiver = SEntMan.GetComponent<ApcPowerReceiverComponent>(ent);
        var powerState = SEntMan.GetComponent<PowerStateComponent>(ent);

        await Server.WaitAssertion(() =>
        {
            _powerStateSys.SetWorkingState((ent, powerState), true);

            _powerStateSys.SetWorkingState((ent, powerState), false);
        });

        await Server.WaitRunTicks(1);

        await Server.WaitAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(powerState.IsTurnedOn, Is.False);
                Assert.That(powerState.IsOperating, Is.False);
                Assert.That(receiver.Load, Is.EqualTo(powerState.IdlePowerDraw).Within(0.01f));
            }

            var operatingChangedEvents = _operatingChangedListener.GetEvents(ent)
                                                              .ToArray();
            Assert.That(operatingChangedEvents, Has.Exactly(2).Items, "Entity is unpowered so IsPowered was not changed.");
            Assert.That(operatingChangedEvents.Last().IsOperating, Is.False);
        });
    }

    private sealed class TurnedOnChangedListenerSystem : TestListenerSystem<TurnedOnStateChanged>;
    private sealed class OperatingChangedListenerSystem : TestListenerSystem<OperatingStateChanged>;
}


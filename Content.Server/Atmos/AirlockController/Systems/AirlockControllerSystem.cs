using Content.Server.Atmos.AirlockController.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.AirlockController;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork.Systems;
using Content.Shared.Doors;
using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Atmos.AirlockController.Systems;

/// <summary>
///     Airlock cycle. Five states + cancel and stall flags
///     Vents, sensors, doors and cycler panels are all device-network ents
///     Signal outputs are extras for player shenanigans
/// </summary>
public sealed partial class AirlockControllerSystem : SharedAirlockControllerSystem
{
    [Dependency] private AirlockCycleStatusSystem _status = default!;
    [Dependency] private AtmosDeviceNetworkSystem _atmosDevNet = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private DeviceLinkSystem _signal = default!;
    [Dependency] private DeviceListSystem _deviceList = default!;
    [Dependency] private PowerReceiverSystem _power = default!;
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockControllerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AirlockControllerComponent, DeviceNetworkPacketEvent>(OnPacketRecv);
        SubscribeLocalEvent<AirlockControllerComponent, DeviceListUpdateEvent>(OnDeviceListUpdate);
        SubscribeLocalEvent<AirlockControllerComponent, ExaminedEvent>(OnExamine);

        InitializeUi();
    }

    private void OnInit(Entity<AirlockControllerComponent> ent, ref ComponentInit args)
    {
        var comp = ent.Comp;

        _signal.EnsureSourcePorts(ent, comp.StateAPort, comp.StateBPort, comp.CyclingPort);
    }

    private void OnExamine(Entity<AirlockControllerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        _status.Examine(GetStatus(ent), args);
    }

    #region Device network

    private void OnDeviceListUpdate(Entity<AirlockControllerComponent> ent, ref DeviceListUpdateEvent args)
    {
        var comp = ent.Comp;

        foreach (var device in args.OldDevices)
        {
            if (args.Devices.Contains(device))
                continue;

            if (!TryComp<DeviceNetworkComponent>(device, out var net))
                continue;

            comp.VentData.Remove(net.Address);
            comp.ScrubberData.Remove(net.Address);
            comp.SensorData.Remove(net.Address);
            comp.DoorReports.Remove(net.Address);

            // Panels cache their binding
            if (comp.CyclerRoles.Remove(device))
            {
                OnCyclerUnassigned((ent, comp), device);
                Dirty(ent);
            }
        }

        _atmosDevNet.Register(ent, null);
        _atmosDevNet.Sync(ent, null);
    }

    private void OnPacketRecv(Entity<AirlockControllerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        var comp = ent.Comp;

        if (!args.Data.TryGetValue(DeviceNetworkConstants.Command, out string? cmd))
            return;

        if (!_deviceList.ExistsInDeviceList(ent, args.SenderAddress))
            return;

        if (cmd == DoorNetworkCommands.Status)
        {
            var report = new AirlockDoorReport();

            if (args.Data.TryGetValue(DoorNetworkCommands.StatusOpen, out bool open))
                report.Open = open;

            if (args.Data.TryGetValue(DoorNetworkCommands.StatusBolted, out bool bolted))
                report.Bolted = bolted;

            if (args.Data.TryGetValue(DoorNetworkCommands.StatusBoltable, out bool boltable))
                report.Boltable = boltable;

            comp.DoorReports[args.SenderAddress] = report;
            comp.ReportsChanged = true;
            return;
        }

        if (cmd != AtmosDeviceNetworkSystem.SyncData
            || !args.Data.TryGetValue(AtmosDeviceNetworkSystem.SyncData, out IAtmosDeviceData? data))
        {
            return;
        }

        switch (data)
        {
            case GasVentPumpData ventData:
                comp.VentData[args.SenderAddress] = ventData;
                break;
            case GasVentScrubberData scrubberData:
                comp.ScrubberData[args.SenderAddress] = scrubberData;
                break;
            case AtmosSensorData sensorData:
                comp.SensorData[args.SenderAddress] = sensorData;
                break;
            default:
                return;
        }
    }

    #endregion

    #region Door assignment

    /// <summary>
    ///     Mapping runs before map init, used to skip to the config menu directly
    /// </summary>
    private bool IsMapping(EntityUid uid)
    {
        return LifeStage(uid) < EntityLifeStage.MapInitialized;
    }

    /// <summary>
    ///     Drops config entries for devices that are gone.
    /// </summary>
    private void PruneCaches(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        var dropped = PruneDeleted(comp.VentRoles)
                      | PruneDeleted(comp.DoorRoles)
                      | PruneDeleted(comp.CyclerRoles);

        if (comp.TargetSensors.TryGetValue(AirlockSide.A, out var sensorA) && TerminatingOrDeleted(sensorA))
            dropped |= comp.TargetSensors.Remove(AirlockSide.A);

        if (comp.TargetSensors.TryGetValue(AirlockSide.B, out var sensorB) && TerminatingOrDeleted(sensorB))
            dropped |= comp.TargetSensors.Remove(AirlockSide.B);

        if (dropped)
            Dirty(ent);
    }

    private bool PruneDeleted<T>(Dictionary<EntityUid, T> config)
    {
        List<EntityUid>? gone = null;

        foreach (var device in config.Keys)
        {
            if (!TerminatingOrDeleted(device))
                continue;

            gone ??= new List<EntityUid>();
            gone.Add(device);
        }

        if (gone == null)
            return false;

        foreach (var device in gone)
        {
            config.Remove(device);
        }

        return true;
    }

    /// <summary>
    ///     Assigned doors that are still bound to us
    /// </summary>
    private List<(EntityUid Door, string Address, AirlockSide Side)> BoundDoors(Entity<AirlockControllerComponent> ent)
    {
        var result = new List<(EntityUid, string, AirlockSide)>();

        foreach (var (address, device) in _deviceList.GetDeviceList(ent.Owner))
        {
            if (ent.Comp.DoorRoles.TryGetValue(device, out var side))
                result.Add((device, address, side));
        }

        return result;
    }

    /// <summary>
    ///     Check if we got both side doors to cycle
    /// </summary>
    private bool CanCycle(Entity<AirlockControllerComponent> ent)
    {
        var hasA = false;
        var hasB = false;

        foreach (var (_, _, side) in BoundDoors(ent))
        {
            hasA |= side == AirlockSide.A;
            hasB |= side == AirlockSide.B;
        }

        return hasA && hasB;
    }

    private void SendDoorCommand(Entity<AirlockControllerComponent> ent, EntityUid door, string address, string command)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var net) || net.ReceiveFrequency == null)
            return;

        if (!TryComp<DeviceNetworkComponent>(door, out var doorNet) || doorNet.ReceiveFrequency == null)
            return;

        var payload = new NetworkPayload
        {
            [DeviceNetworkConstants.Command] = command,
            [DoorNetworkCommands.ReplyNetId] = net.DeviceNetId,
            [DoorNetworkCommands.ReplyFrequency] = net.ReceiveFrequency.Value,
        };

        _deviceNetwork.QueuePacket(ent, address, payload, doorNet.ReceiveFrequency.Value, doorNet.DeviceNetId);
    }

    #endregion

    #region Cycle requests

    /// <summary>
    ///     A panel is just the cycle button of the side it was assigned to
    /// </summary>
    public bool TryRequestCycleFrom(Entity<AirlockControllerComponent> ent, EntityUid cycler, EntityUid user)
    {
        if (!ent.Comp.CyclerRoles.TryGetValue(cycler, out var side))
            return false;

        return TryRequestCycle(ent, side, user, cycler);
    }

    private bool TryRequestCycle(
        Entity<AirlockControllerComponent> ent,
        AirlockSide side,
        EntityUid? user = null,
        EntityUid? source = null)
    {
        var comp = ent.Comp;

        if (comp.MaintenanceMode)
            return false;

        if (comp.State != AirlockCycleState.Idle)
            return false;

        if (comp.CurrentSide == side)
            return false;

        // Check access on the side. Source so it pops error on cycler or panel
        if (user != null && !CanUseSide(ent, side, user.Value))
        {
            DenyAccess(source ?? ent.Owner, user.Value);
            return false;
        }

        // Error out if doors are missing
        if (!CanCycle(ent))
        {
            comp.StallReason = AirlockStallReason.MissingDoors;
            UpdateUi(ent);
            return false;
        }

        comp.TargetSide = side;
        comp.CancelRequested = false;
        comp.StallReason = null;

        // Sealing shuts and bolts everything already
        comp.RestoreDoors = false;

        if (user != null)
            LogUse(ent, user.Value);

        SetState(ent, AirlockCycleState.Sealing);
        return true;
    }

    /// <summary>
    ///     Adds the user to our own access log
    /// </summary>
    private void LogUse(Entity<AirlockControllerComponent> ent, EntityUid user)
    {
        if (_tag.HasTag(user, PreventAccessLoggingTag))
            return;

        if (TryComp<AccessReaderComponent>(ent, out var reader))
            _access.LogAccess((ent, reader), user);
    }

    private static readonly ProtoId<TagPrototype> PreventAccessLoggingTag = "PreventAccessLogging";

    private void DenyAccess(EntityUid uid, EntityUid user)
    {
        _popup.PopupEntity(Loc.GetString("airlock-controller-access-denied"), uid, user);
        _audio.PlayPvs(DenySound, uid);
    }

    private static readonly SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    private bool CanUseSide(Entity<AirlockControllerComponent> ent, AirlockSide side, EntityUid user)
    {
        foreach (var (door, _, doorSide) in BoundDoors(ent))
        {
            if (doorSide != side)
                continue;

            if (!IsAllowedQuiet(user, door))
                return false;
        }

        return true;
    }

    /// <summary>
    ///     First press goes to the side the chamber already matches.
    ///     Second press unbolts immediately.
    /// </summary>
    private void RequestCancel(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.MaintenanceMode)
            return;

        // Unsealing has already released target side at this point
        if (comp.State is AirlockCycleState.Idle or AirlockCycleState.Unsealing)
            return;

        // Whichever mix the chamber holds, aka which side we can go to safely without mixing atmoses
        var safe = comp.State == AirlockCycleState.Filling
            ? comp.TargetSide
            : comp.CurrentSide;

        if (comp.CancelRequested)
        {
            comp.TargetSide = safe;
            SetState(ent, AirlockCycleState.Unsealing);
            return;
        }

        comp.CancelRequested = true;
        comp.TargetSide = safe;

        // Straight unseal if pumps aren't started yet
        if (comp.State == AirlockCycleState.Sealing)
            SetState(ent, AirlockCycleState.Unsealing);
    }

    #endregion

    #region State machine

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<AirlockControllerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var cycling = !comp.MaintenanceMode && comp.State != AirlockCycleState.Idle;
            var restoring = !comp.MaintenanceMode && comp.RestoreDoors;

            if (now >= comp.NextSync)
            {
                comp.NextSync = now + comp.UpdateInterval;
                UpdateAtmosPace((uid, comp), now, cycling, restoring);
            }

            // Doors are slow to answer
            if (!comp.ReportsChanged || !WaitingOnDoors(comp))
                continue;

            comp.ReportsChanged = false;

            if (restoring)
                UpdateDoorRestore((uid, comp));

            if (cycling)
                UpdateCycle((uid, comp), now);
        }
    }

    /// <summary>
    ///     States where a door reply is the only thing we're waiting for
    /// </summary>
    private static bool WaitingOnDoors(AirlockControllerComponent comp)
    {
        if (comp.MaintenanceMode)
            return false;

        return comp.RestoreDoors
               || comp.State is AirlockCycleState.Sealing or AirlockCycleState.Unsealing;
    }

    /// <summary>
    ///     Housekeeping, sensors and the pumping states
    /// </summary>
    private void UpdateAtmosPace(Entity<AirlockControllerComponent> ent, TimeSpan now, bool cycling, bool restoring)
    {
        PruneCaches(ent);
        _status.Apply(ent, GetStatus(ent));
        UpdateCyclers(ent);

        var uiOpen = UserInterfaceSystem.IsUiOpen(ent.Owner, AirlockControllerUiKey.Key)
                     || UserInterfaceSystem.IsUiOpen(ent.Owner, AirlockControllerUiKey.Config);

        if (cycling || uiOpen)
            _atmosDevNet.Sync(ent, null);

        if (uiOpen)
            UpdateUi(ent);

        if (!cycling && !restoring)
            return;

        // Catches a door that went quiet
        PollDoors(ent);

        // Seal check while pumping
        if (cycling && !WaitingOnDoors(ent.Comp))
            UpdateCycle(ent, now);
    }

    /// <summary>
    ///     Doors won't sent us updates so just ask.
    /// </summary>
    private void PollDoors(Entity<AirlockControllerComponent> ent)
    {
        foreach (var (door, address, _) in BoundDoors(ent))
        {
            SendDoorCommand(ent, door, address, DoorNetworkCommands.Sync);
        }
    }

    /// <summary>
    ///     Goes back to Idle state: This side unbolted, other side closed and bolted.
    /// </summary>
    private void UpdateDoorRestore(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;
        var far = comp.CurrentSide == AirlockSide.A ? AirlockSide.B : AirlockSide.A;

        var farDoors = 0;
        var farClosed = 0;
        var farBolted = 0;
        var nearDoors = 0;
        var nearReleased = 0;

        foreach (var (_, address, side) in BoundDoors(ent))
        {
            var known = comp.DoorReports.TryGetValue(address, out var report);

            if (side == far)
            {
                farDoors++;
                if (!known)
                    continue;

                if (!report.Open)
                    farClosed++;

                if (report.Bolted || !report.Boltable)
                    farBolted++;
            }
            else
            {
                nearDoors++;
                if (known && (!report.Bolted || !report.Boltable))
                    nearReleased++;
            }
        }

        if (nearReleased < nearDoors)
        {
            if (comp.BoltingEnabled)
                CommandSide(ent, comp.CurrentSide, DoorNetworkCommands.Unbolt);

            return;
        }

        if (farClosed < farDoors)
        {
            CommandSide(ent, far, DoorNetworkCommands.Close);
            return;
        }

        if (comp.BoltingEnabled && farBolted < farDoors)
        {
            CommandSide(ent, far, DoorNetworkCommands.Bolt);
            return;
        }

        comp.RestoreDoors = false;
        UpdateUi(ent);
    }

    private void UpdateCycle(Entity<AirlockControllerComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;

        if (!CanCycle(ent))
        {
            comp.StallReason = AirlockStallReason.MissingDoors;
            return;
        }

        // Re-checked doors for the whole cycle, error if broken
        if (comp.State is AirlockCycleState.Evacuating or AirlockCycleState.Filling
            && !IsSealed(ent))
        {
            comp.StallReason = AirlockStallReason.SealLost;
            return;
        }

        switch (comp.State)
        {
            case AirlockCycleState.Sealing:
                UpdateSealing(ent, now);
                break;
            case AirlockCycleState.Evacuating:
            case AirlockCycleState.Filling:
                UpdatePumping(ent, now);
                break;
            case AirlockCycleState.Unsealing:
                UpdateUnsealing(ent, now);
                break;
        }
    }

    /// <summary>
    ///     Shuts every door, then bolts. Doors bolted open can't close, unbolt first.
    /// </summary>
    private void UpdateSealing(Entity<AirlockControllerComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;

        // Doors that are where we want them, stall only when nothing moves
        var progress = 0;
        var allSealed = true;

        foreach (var (door, address, _) in BoundDoors(ent))
        {
            // No report yet, PollDoors will ask again
            if (!comp.DoorReports.TryGetValue(address, out var report))
            {
                allSealed = false;
                continue;
            }

            if (report.Open)
            {
                SendDoorCommand(ent, door, address, report.Bolted
                    ? DoorNetworkCommands.Unbolt
                    : DoorNetworkCommands.Close);

                allSealed = false;
                continue;
            }

            progress++;

            // Shut. Bolt unless it has no bolts or the wire is cut
            if (!comp.BoltingEnabled || !report.Boltable || report.Bolted)
            {
                progress++;
                continue;
            }

            SendDoorCommand(ent, door, address, DoorNetworkCommands.Bolt);
            allSealed = false;
        }

        if (!allSealed)
        {
            CheckProgress(ent, progress, now, AirlockStallReason.NotSealing);
            return;
        }

        SetState(ent, AirlockCycleState.Evacuating);
    }

    private void UpdatePumping(Entity<AirlockControllerComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;
        var siphon = comp.State == AirlockCycleState.Evacuating;

        // Re-sent every tick or vents can get status stuck
        ApplyVents(ent, siphon);

        if (!TryGetChamberPressure(ent, out var reading))
        {
            comp.StallReason = AirlockStallReason.NoSensors;
            return;
        }

        // The fullest tile is the one still holding gas, the emptiest the one still filling
        var gate = siphon ? reading.Max : reading.Min;

        var reached = siphon
            ? gate <= comp.EvacuatedPressure
            : gate >= GetTargetPressure(ent, comp.TargetSide) - comp.PressureTolerance;

        if (reached)
        {
            if (HasSettled(ent, gate))
                SetState(ent, siphon ? AirlockCycleState.Filling : AirlockCycleState.Unsealing);

            return;
        }

        ResetSettle(ent);
        CheckProgress(ent, gate, now, AirlockStallReason.NotProgressing);
    }

    /// <summary>
    ///     If gas is still changing atmos is still churning. Wait for it to settle.
    ///     Has a max waiting time just in case.
    /// </summary>
    private static bool HasSettled(Entity<AirlockControllerComponent> ent, float gate)
    {
        var comp = ent.Comp;

        var settled = !float.IsNaN(comp.LastChamberReading)
                      && MathF.Abs(gate - comp.LastChamberReading) <= comp.SettleTolerance;

        comp.LastChamberReading = gate;

        return settled || ++comp.SettleTicks >= comp.MaxSettleTicks;
    }

    private static void ResetSettle(Entity<AirlockControllerComponent> ent)
    {
        ent.Comp.SettleTicks = 0;
        ent.Comp.LastChamberReading = float.NaN;
    }

    /// <summary>
    ///     Unbolts the target side then opens it.
    /// </summary>
    private void UpdateUnsealing(Entity<AirlockControllerComponent> ent, TimeSpan now)
    {
        var comp = ent.Comp;
        var progress = 0;
        var total = 0;

        foreach (var (door, address, side) in BoundDoors(ent))
        {
            if (side != comp.TargetSide)
                continue;

            total++;

            if (!comp.DoorReports.TryGetValue(address, out var report))
                continue;

            // A bolted door ignores Open
            if (report.Boltable && report.Bolted)
            {
                if (comp.BoltingEnabled)
                    SendDoorCommand(ent, door, address, DoorNetworkCommands.Unbolt);

                continue;
            }

            progress++;

            if (!report.Open)
            {
                SendDoorCommand(ent, door, address, DoorNetworkCommands.Open);
                continue;
            }

            progress++;
        }

        if (progress < total * 2)
        {
            CheckProgress(ent, progress, now, AirlockStallReason.NotOpening);
            return;
        }

        // Update to new side state
        comp.CurrentSide = comp.TargetSide;
        comp.CancelRequested = false;
        comp.StallReason = null;
        SetState(ent, AirlockCycleState.Idle);
    }

    /// <summary>
    ///     Checks for stalling during progress, e.g. spacing, broken pipes..
    /// </summary>
    private void CheckProgress(Entity<AirlockControllerComponent> ent, float value, TimeSpan now, AirlockStallReason reason)
    {
        var comp = ent.Comp;

        if (float.IsNaN(comp.LastProgressValue)
            || MathF.Abs(value - comp.LastProgressValue) > comp.PressureTolerance * 0.1f)
        {
            comp.LastProgressValue = value;
            comp.LastProgressTime = now;
            comp.StallReason = null;
            return;
        }

        if (now - comp.LastProgressTime >= comp.StallTimeout)
            comp.StallReason = reason;
    }

    private void SetState(Entity<AirlockControllerComponent> ent, AirlockCycleState state)
    {
        var comp = ent.Comp;

        comp.State = state;
        comp.LastProgressValue = float.NaN;
        comp.LastProgressTime = _timing.CurTime;
        ResetSettle(ent);

        // Whatever the last state heard doesn't answer this one
        comp.ReportsChanged = false;

        switch (state)
        {
            case AirlockCycleState.Sealing:
                // Need fresh reports for doors
                comp.DoorReports.Clear();
                CommandAllDoors(ent, DoorNetworkCommands.Close);
                StopVents(ent);
                break;

            case AirlockCycleState.Evacuating:
                ApplyVents(ent, siphon: true);
                break;

            case AirlockCycleState.Filling:
                ApplyVents(ent, siphon: false);
                break;

            case AirlockCycleState.Unsealing:
                StopVents(ent);
                // Don't unbolt if cut wire
                if (comp.BoltingEnabled)
                    CommandSide(ent, comp.TargetSide, DoorNetworkCommands.Unbolt);
                break;

            case AirlockCycleState.Idle:
                StopVents(ent);
                break;
        }

        // A door already in position never pushes
        if (WaitingOnDoors(comp))
            PollDoors(ent);

        UpdateOutputs(ent);
        UpdateUi(ent);
    }

    #endregion

    #region Doors

    /// <summary>
    ///     True when every assigned door reports shut, and bolted where it can be.
    /// </summary>
    private bool IsSealed(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        foreach (var (_, address, _) in BoundDoors(ent))
        {
            if (!comp.DoorReports.TryGetValue(address, out var report) || report.Open)
                return false;

            // Boltless doors can't be waited on for bolting
            if (comp.BoltingEnabled && report.Boltable && !report.Bolted)
                return false;
        }

        return true;
    }

    private void CommandAllDoors(Entity<AirlockControllerComponent> ent, string command)
    {
        foreach (var (door, address, _) in BoundDoors(ent))
        {
            SendDoorCommand(ent, door, address, command);
        }
    }

    private void CommandSide(Entity<AirlockControllerComponent> ent, AirlockSide side, string command)
    {
        foreach (var (door, address, doorSide) in BoundDoors(ent))
        {
            if (doorSide == side)
                SendDoorCommand(ent, door, address, command);
        }
    }

    #endregion

    #region Atmos devices

    private bool TryGetChamberPressure(Entity<AirlockControllerComponent> ent, out AirlockChamberReading reading)
    {
        var comp = ent.Comp;

        var min = float.MaxValue;
        var max = float.MinValue;
        var total = 0f;
        var count = 0;

        foreach (var (address, device) in _deviceList.GetDeviceList(ent.Owner))
        {
            // Exclude outside sensors
            if (!_power.IsPowered(device) || comp.TargetSensors.ContainsValue(device))
                continue;

            if (!comp.SensorData.TryGetValue(address, out var sensor))
                continue;

            min = MathF.Min(min, sensor.Pressure);
            max = MathF.Max(max, sensor.Pressure);
            total += sensor.Pressure;
            count++;
        }

        if (count == 0)
        {
            reading = default;
            return false;
        }

        reading = new AirlockChamberReading(min, max, total / count);
        return true;
    }

    private float GetTargetPressure(Entity<AirlockControllerComponent> ent, AirlockSide side)
    {
        var comp = ent.Comp;

        // Preset or target sensor mode? Use preset if sensor died
        if (comp.TargetSensors.TryGetValue(side, out var sensor)
            && _power.IsPowered(sensor)
            && TryComp<DeviceNetworkComponent>(sensor, out var net)
            && comp.SensorData.TryGetValue(net.Address, out var data))
        {
            return data.Pressure;
        }

        return side == AirlockSide.A ? comp.PresetPressureA : comp.PresetPressureB;
    }

    /// <summary>
    ///     Check which vents we can use for our current mode and uses them accordingly
    /// </summary>
    private void ApplyVents(Entity<AirlockControllerComponent> ent, bool siphon)
    {
        var comp = ent.Comp;

        // We want the other side air: Evacuating siphons towards current side, filling comes from target side
        var side = siphon ? comp.CurrentSide : comp.TargetSide;

        var wanted = (siphon, side) switch
        {
            (true, AirlockSide.A) => AirlockVentRole.SiphonA,
            (true, AirlockSide.B) => AirlockVentRole.SiphonB,
            (false, AirlockSide.A) => AirlockVentRole.VentA,
            _ => AirlockVentRole.VentB,
        };

        var target = siphon ? 0f : GetTargetPressure(ent, side);
        var used = false;

        // Everything registered gets a command, assigned or not
        foreach (var (address, device) in _deviceList.GetDeviceList(ent.Owner))
        {
            if (!_power.IsPowered(device))
                continue;

            comp.VentRoles.TryGetValue(device, out var roles);

            var wants = (roles & wanted) != 0;

            if (comp.VentData.ContainsKey(address))
            {
                SetVent(ent, address, wants && !siphon
                    ? Vent(true, VentPumpDirection.Releasing, target)
                    : Vent(wants, VentPumpDirection.Siphoning));

                used |= wants;
            }
            else if (comp.ScrubberData.ContainsKey(address))
            {
                // Scrubbers can only siphon
                var useScrubber = wants && siphon;
                SetScrubber(ent, address, Scrubber(useScrubber));
                used |= useScrubber;
            }
        }

        comp.StallReason = used ? null : AirlockStallReason.NoUsableVent;
    }

    private void StopVents(Entity<AirlockControllerComponent> ent)
    {
        foreach (var (address, _) in _deviceList.GetDeviceList(ent.Owner))
        {
            if (ent.Comp.VentData.ContainsKey(address))
                SetVent(ent, address, Vent(false, VentPumpDirection.Siphoning));
            else if (ent.Comp.ScrubberData.ContainsKey(address))
                SetScrubber(ent, address, Scrubber(false));
        }
    }

    private void SetScrubber(Entity<AirlockControllerComponent> ent, string address, GasVentScrubberData data)
    {
        ent.Comp.ScrubberData[address] = data;
        _atmosDevNet.SetDeviceState(ent, address, data);
        _atmosDevNet.Sync(ent, address);
    }

    private void SetVent(Entity<AirlockControllerComponent> ent, string address, GasVentPumpData data)
    {
        ent.Comp.VentData[address] = data;
        _atmosDevNet.SetDeviceState(ent, address, data);
        _atmosDevNet.Sync(ent, address);
    }

    // PressureLockoutOverride needed otherwise stuff won't work in vacuum
    private static GasVentPumpData Vent(bool enabled, VentPumpDirection direction, float bound = Atmospherics.OneAtmosphere) => new()
    {
        Enabled = enabled,
        Dirty = true,
        IgnoreAlarms = true,
        PumpDirection = direction,
        PressureChecks = direction == VentPumpDirection.Releasing
            ? VentPressureBound.ExternalBound
            : VentPressureBound.NoBound,
        ExternalPressureBound = bound,
        PressureLockoutOverride = true,
    };

    /// <summary>
    ///     Siphons everything, wide net cause airlocks are boring to wait in
    /// </summary>
    private static GasVentScrubberData Scrubber(bool enabled) => new()
    {
        Enabled = enabled,
        Dirty = true,
        IgnoreAlarms = true,
        PumpDirection = ScrubberPumpDirection.Siphoning,
        WideNet = enabled,
    };

    #endregion

    #region Outputs

    private void UpdateOutputs(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;
        var idle = comp.State == AirlockCycleState.Idle;

        // Flashy light signal wire can be cut
        _signal.SendSignal(ent, comp.CyclingPort, !idle && comp.EmergencyLightsEnabled);
        _signal.SendSignal(ent, comp.StateAPort, idle && comp.CurrentSide == AirlockSide.A);
        _signal.SendSignal(ent, comp.StateBPort, idle && comp.CurrentSide == AirlockSide.B);

        _status.Apply(ent, GetStatus(ent));
        UpdateCyclers(ent);
    }

    private static AirlockCycleStatus GetStatus(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        return new AirlockCycleStatus(
            comp.State,
            comp.CurrentSide,
            comp.StallReason,
            comp.MaintenanceMode,
            IsWarning(ent));
    }

    /// <summary>
    ///     Pushes our state to the panels, and binds them
    /// </summary>
    private void UpdateCyclers(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.CyclerRoles.Count == 0)
            return;

        var pressure = TryGetChamberPressure(ent, out var reading) ? reading.Mean : (float?)null;
        var status = GetStatus(ent);

        foreach (var device in _deviceList.GetDeviceList(ent.Owner).Values)
        {
            if (!comp.CyclerRoles.TryGetValue(device, out var side)
                || !TryComp<AirlockCyclerComponent>(device, out var panel))
            {
                continue;
            }

            panel.Controller = ent.Owner;
            panel.Side = side;
            panel.Status = status;
            panel.ChamberPressure = pressure;

            _status.Apply(device, status);
        }
    }

    /// <summary>
    ///     Checks for wire muting light and sound.
    /// </summary>
    private static bool IsWarning(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        return comp.EmergencyLightsEnabled
               && !comp.MaintenanceMode
               && comp.State != AirlockCycleState.Idle;
    }

    #endregion

    #region Maintenance

    /// <summary>
    ///     Force side for atmos techs who know what they're doing
    /// </summary>
    public void ForceSide(Entity<AirlockControllerComponent> ent, AirlockSide side)
    {
        var comp = ent.Comp;

        comp.CurrentSide = side;
        comp.TargetSide = side;
        comp.CancelRequested = false;
        comp.StallReason = null;
        SetState(ent, AirlockCycleState.Idle);

        if (!comp.MaintenanceMode)
            StartDoorRestore(ent);
    }

    /// <summary>
    ///     Drop door reports to get fresh ones to work with
    /// </summary>
    private void StartDoorRestore(Entity<AirlockControllerComponent> ent)
    {
        ent.Comp.DoorReports.Clear();
        ent.Comp.RestoreDoors = true;

        // A door already in position never pushes
        PollDoors(ent);
    }

    /// <summary>
    ///     Cutting/pulsing the bolting wire
    /// </summary>
    public void SetBoltingEnabled(Entity<AirlockControllerComponent> ent, bool enabled)
    {
        ent.Comp.BoltingEnabled = enabled;
    }

    public void SetEmergencyLightsEnabled(Entity<AirlockControllerComponent> ent, bool enabled)
    {
        ent.Comp.EmergencyLightsEnabled = enabled;
        UpdateOutputs(ent);
    }

    /// <summary>
    ///     Runs after the flag is already set, shared handler will predict
    /// </summary>
    private void ApplyMaintenanceMode(Entity<AirlockControllerComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.MaintenanceMode)
        {
            StopVents(ent);
            comp.CancelRequested = false;
            comp.StallReason = null;
            comp.RestoreDoors = false;
            SetState(ent, AirlockCycleState.Idle);
            CommandAllDoors(ent, DoorNetworkCommands.Unbolt);
            return;
        }

        // Will restore all doors to the current side
        StartDoorRestore(ent);
    }

    #endregion
}

/// <summary>
///     What the chamber sensors say, min/max/mean used in different phases
/// </summary>
public readonly record struct AirlockChamberReading(float Min, float Max, float Mean);

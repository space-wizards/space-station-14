using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;

namespace Content.Shared.Atmos.AirlockController;

/// <summary>
///     Config edits handled here for prediction.
/// </summary>
public abstract partial class SharedAirlockControllerSystem : EntitySystem
{
    [Dependency] protected AccessReaderSystem Access = default!;
    [Dependency] protected SharedUserInterfaceSystem UserInterfaceSystem = default!;

    private const AirlockVentRole AllVentRoles =
        AirlockVentRole.VentA | AirlockVentRole.SiphonA | AirlockVentRole.VentB | AirlockVentRole.SiphonB;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<AirlockControllerComponent>(AirlockControllerUiKey.Config,
            subs =>
        {
            subs.Event<AirlockControllerSetVentRolesMessage>(OnSetVentRoles);
            subs.Event<AirlockControllerSetDoorSideMessage>(OnSetDoorSide);
            subs.Event<AirlockControllerSetCyclerSideMessage>(OnSetCyclerSide);
            subs.Event<AirlockControllerSetTargetSensorMessage>(OnSetTargetSensor);
            subs.Event<AirlockControllerSetPresetMessage>(OnSetPreset);
            subs.Event<AirlockControllerSetMaintenanceMessage>(OnSetMaintenance);
        });
    }

    #region Hooks

    /// <summary>
    ///     Refresh whatever is showing this controller.
    /// </summary>
    protected virtual void UpdateUi(Entity<AirlockControllerComponent> ent)
    {
    }

    /// <summary>
    ///     Assumed yes, the window only opens for someone who already passed
    /// </summary>
    protected virtual bool CanEdit(Entity<AirlockControllerComponent> ent, EntityUid actor)
    {
        return true;
    }

    /// <summary>
    ///     Whether the device is bound and is the kind of thing fitting the role. Serverside.
    /// </summary>
    protected virtual bool IsValidDevice(
        Entity<AirlockControllerComponent> ent,
        EntityUid device,
        AirlockDeviceKind kind,
        EntityUid actor)
    {
        return true;
    }

    /// <summary>
    ///     Doors are polled over the device network, serverside.
    /// </summary>
    protected virtual void OnDoorAssigned(Entity<AirlockControllerComponent> ent, EntityUid door)
    {
    }

    protected virtual void OnDoorUnassigned(Entity<AirlockControllerComponent> ent, EntityUid door)
    {
    }

    /// <summary>
    ///     Tells a dropped panel it's no longer claimed
    /// </summary>
    protected virtual void OnCyclerUnassigned(Entity<AirlockControllerComponent> ent, EntityUid cycler)
    {
    }

    /// <summary>
    ///     Entering and leaving maintenance drives the cycle and the doors.
    /// </summary>
    protected virtual void OnMaintenanceChanged(Entity<AirlockControllerComponent> ent)
    {
    }

    #endregion

    protected static bool IsValidSide(AirlockSide side)
    {
        return side is AirlockSide.A or AirlockSide.B;
    }

    /// <summary>
    ///     Access check that writes no log entry
    /// </summary>
    public bool IsAllowedQuiet(EntityUid user, EntityUid target)
    {
        if (!TryComp<AccessReaderComponent>(target, out var reader))
            return true;

        var sources = Access.FindPotentialAccessItems(user);
        var tags = Access.FindAccessTags(user, sources);
        Access.FindStationRecordKeys(user, out var keys, sources);

        return Access.IsAllowed(tags, keys, target, reader);
    }

    private void OnSetVentRoles(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetVentRolesMessage args)
    {
        if (!CanEdit(ent, args.Actor))
            return;

        if (!TryGetEntity(args.Device, out var vent)
            || !IsValidDevice(ent, vent.Value, AirlockDeviceKind.Vent, args.Actor))
        {
            return;
        }

        var roles = args.Roles & AllVentRoles;

        if (roles == AirlockVentRole.None)
            ent.Comp.VentRoles.Remove(vent.Value);
        else
            ent.Comp.VentRoles[vent.Value] = roles;

        Dirty(ent);
        UpdateUi(ent);
    }

    // Side A, side B
    private void OnSetDoorSide(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetDoorSideMessage args)
    {
        if (!CanEdit(ent, args.Actor))
            return;

        if (!TryGetEntity(args.Device, out var door))
            return;

        if (args.Side is not { } side)
        {
            if (ent.Comp.DoorRoles.Remove(door.Value))
            {
                OnDoorUnassigned(ent, door.Value);
                Dirty(ent);
                UpdateUi(ent);
            }

            return;
        }

        if (!IsValidSide(side) || !IsValidDevice(ent, door.Value, AirlockDeviceKind.Door, args.Actor))
            return;

        ent.Comp.DoorRoles[door.Value] = side;
        OnDoorAssigned(ent, door.Value);

        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnSetCyclerSide(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetCyclerSideMessage args)
    {
        if (!CanEdit(ent, args.Actor))
            return;

        if (!TryGetEntity(args.Device, out var cycler))
            return;

        if (args.Side is not { } side)
        {
            if (ent.Comp.CyclerRoles.Remove(cycler.Value))
            {
                OnCyclerUnassigned(ent, cycler.Value);
                Dirty(ent);
                UpdateUi(ent);
            }

            return;
        }

        if (!IsValidSide(side) || !IsValidDevice(ent, cycler.Value, AirlockDeviceKind.Cycler, args.Actor))
            return;

        ent.Comp.CyclerRoles[cycler.Value] = side;

        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnSetTargetSensor(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetTargetSensorMessage args)
    {
        if (!CanEdit(ent, args.Actor) || !IsValidSide(args.Side))
            return;

        var comp = ent.Comp;

        if (args.Device is not { } netSensor)
        {
            comp.TargetSensors.Remove(args.Side);
        }
        else if (TryGetEntity(netSensor, out var sensor)
                 && IsValidDevice(ent, sensor.Value, AirlockDeviceKind.Sensor, args.Actor))
        {
            var other = args.Side == AirlockSide.A ? AirlockSide.B : AirlockSide.A;

            // Swap sensors if picking the one on the other side
            if (comp.TargetSensors.TryGetValue(other, out var taken) && taken == sensor.Value)
            {
                if (comp.TargetSensors.TryGetValue(args.Side, out var ours))
                    comp.TargetSensors[other] = ours;
                else
                    comp.TargetSensors.Remove(other);
            }

            comp.TargetSensors[args.Side] = sensor.Value;
        }
        else
        {
            return;
        }

        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnSetPreset(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetPresetMessage args)
    {
        if (!CanEdit(ent, args.Actor))
            return;

        // Make sure the pressure makes sense, NaN check
        if (!IsValidSide(args.Side) || !float.IsFinite(args.Pressure))
            return;

        var pressure = Math.Clamp(args.Pressure, 0f, Atmospherics.MaxOutputPressure);

        if (args.Side == AirlockSide.A)
            ent.Comp.PresetPressureA = pressure;
        else
            ent.Comp.PresetPressureB = pressure;

        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnSetMaintenance(Entity<AirlockControllerComponent> ent, ref AirlockControllerSetMaintenanceMessage args)
    {
        if (!CanEdit(ent, args.Actor) || ent.Comp.MaintenanceMode == args.Enabled)
            return;

        ent.Comp.MaintenanceMode = args.Enabled;
        OnMaintenanceChanged(ent);

        Dirty(ent);
        UpdateUi(ent);
    }
}

/// <summary>
///     What a device has to be for a role to stick.
/// </summary>
public enum AirlockDeviceKind : byte
{
    Vent,
    Door,
    Sensor,
    Cycler,
}

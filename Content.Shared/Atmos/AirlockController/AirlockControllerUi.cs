using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.AirlockController;

/// <summary>
///     One device bound to the controller, with whatever roles it currently has.
/// </summary>
[Serializable, NetSerializable]
public sealed class AirlockDeviceEntry
{
    public NetEntity Device;

    /// <summary>
    ///     Display only
    /// </summary>
    public string Address = string.Empty;

    public string Name = string.Empty;

    public bool IsVent;

    // Scrubbers can only siphon
    public bool IsScrubber;

    public bool IsSensor;
    public bool IsDoor;
    public bool IsCycler;
}

/// <summary>
///     Sensor option to be used outside the chamber on a side
/// </summary>
[Serializable, NetSerializable]
public sealed class AirlockSensorOption
{
    public NetEntity Device;
    public string Name = string.Empty;
}

/// <summary>
///     Main panel view status
/// </summary>
[Serializable, NetSerializable]
public sealed class AirlockControllerUiState : BoundUserInterfaceState
{
    public AirlockCycleStatus Status;
    public bool CancelRequested;

    public float? ChamberPressure;
}

/// <summary>
///     Configuration view status. Only not editable, the rest is predicted
/// </summary>
[Serializable, NetSerializable]
public sealed class AirlockControllerConfigUiState : BoundUserInterfaceState
{
    public List<AirlockDeviceEntry> Devices = new();

    public string Address = string.Empty;

    public int DoorCount;

    /// <summary>
    ///     Sensors left inside the chamber.
    /// </summary>
    public int ChamberSensorCount;

    public AirlockSide CurrentSide;

    /// <summary>
    ///     Everything a side can be pointed at, Custom pressure is null.
    /// </summary>
    public List<AirlockSensorOption> Sensors = new();

    /// <summary>
    ///     What the chosen sensor reads, shown in place of the input.
    /// </summary>
    public float? TargetPressureA;
    public float? TargetPressureB;
}

/// <summary>
///     What the panel mirrors from its controller. Read only, all it can do is ask
/// </summary>
[Serializable, NetSerializable]
public sealed class AirlockCyclerUiState : BoundUserInterfaceState
{
    /// <summary>
    ///     False when no controller has it assigned
    /// </summary>
    public bool Bound;

    /// <summary>
    ///     The side we call the chamber to
    /// </summary>
    public AirlockSide Side;

    public AirlockCycleStatus Status;

    public float? ChamberPressure;
}

[Serializable, NetSerializable]
public sealed class AirlockCyclerCycleMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AirlockControllerCycleMessage : BoundUserInterfaceMessage
{
    public AirlockSide Side;

    public AirlockControllerCycleMessage(AirlockSide side)
    {
        Side = side;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerCancelMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AirlockControllerOpenConfigMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetVentRolesMessage : BoundUserInterfaceMessage
{
    public NetEntity Device;
    public AirlockVentRole Roles;

    public AirlockControllerSetVentRolesMessage(NetEntity device, AirlockVentRole roles)
    {
        Device = device;
        Roles = roles;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetDoorSideMessage : BoundUserInterfaceMessage
{
    public NetEntity Device;

    public AirlockSide? Side;

    public AirlockControllerSetDoorSideMessage(NetEntity device, AirlockSide? side)
    {
        Device = device;
        Side = side;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetCyclerSideMessage : BoundUserInterfaceMessage
{
    public NetEntity Device;

    public AirlockSide? Side;

    public AirlockControllerSetCyclerSideMessage(NetEntity device, AirlockSide? side)
    {
        Device = device;
        Side = side;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetTargetSensorMessage : BoundUserInterfaceMessage
{
    public AirlockSide Side;

    public NetEntity? Device;

    public AirlockControllerSetTargetSensorMessage(AirlockSide side, NetEntity? device)
    {
        Side = side;
        Device = device;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetPresetMessage : BoundUserInterfaceMessage
{
    public AirlockSide Side;
    public float Pressure;

    public AirlockControllerSetPresetMessage(AirlockSide side, float pressure)
    {
        Side = side;
        Pressure = pressure;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerSetMaintenanceMessage : BoundUserInterfaceMessage
{
    public bool Enabled;

    public AirlockControllerSetMaintenanceMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class AirlockControllerForceSideMessage : BoundUserInterfaceMessage
{
    public AirlockSide Side;

    public AirlockControllerForceSideMessage(AirlockSide side)
    {
        Side = side;
    }
}

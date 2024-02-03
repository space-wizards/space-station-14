﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Research;

[Serializable, NetSerializable]
public enum RoboticsConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class RoboticsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Map of device network addresses to cyborg data.
    /// </summary>
    public Dictionary<string, CyborgControlData> Cyborgs;

    /// <summary>
    /// When the destroy button can next be used.
    /// </summary>
    public TimeSpan NextDestroy;

    public RoboticsConsoleState(Dictionary<string, CyborgControlData> cyborgs, TimeSpan nextDestroy)
    {
        Cyborgs = cyborgs;
        NextDestroy = nextDestroy;
    }
}

/// <summary>
/// Message to disable the selected cyborg.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoboticsConsoleDisableMessage : BoundUserInterfaceMessage
{
    public readonly string Address;

    public RoboticsConsoleDisableMessage(string address)
    {
        Address = address;
    }
}

/// <summary>
/// Message to destroy the selected cyborg.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoboticsConsoleDestroyMessage : BoundUserInterfaceMessage
{
    public readonly string Address;

    public RoboticsConsoleDestroyMessage(string address)
    {
        Address = address;
    }
}

/// <summary>
/// All data a client needs to render the console UI for a single cyborg.
/// Created by <c>BorgTransponderComponent</c> and sent to clients by <c>RoboticsConsoleComponent</c>.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public record struct CyborgControlData
{
    /// <summary>
    /// Chassis prototype of the borg.
    /// </summary>
    [DataField]
    public EntProtoId Chassis;

    /// <summary>
    /// Name of the borg's entity, including its silicon id.
    /// </summary>
    [DataField]
    public string Name;

    /// <summary>
    /// Battery charge from 0 to 1.
    /// </summary>
    [DataField]
    public float Charge;

    /// <summary>
    /// How many modules this borg has, just useful information for roboticists.
    /// Lets them keep track of the latejoin borgs that need new modules and stuff.
    /// </summary>
    [DataField]
    public int ModuleCount;

    /// <summary>
    /// Whether the borg has a brain installed or not.
    /// </summary>
    [DataField]
    public bool HasBrain;

    /// <summary>
    /// When this cyborg's data will be deleted.
    /// Set by the console when receiving the packet.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Timeout;
}

public static class RoboticsConsoleConstants
{
    // broadcast by cyborgs on Robotics Console frequency
    public const string NET_CYBORG_DATA = "cyborg-data";

    // sent by robotics console to cyborgs on Cyborg Control frequency
    public const string NET_DISABLE_COMMAND = "cyborg-disable";
    public const string NET_DESTROY_COMMAND = "cyborg-destroy";
}

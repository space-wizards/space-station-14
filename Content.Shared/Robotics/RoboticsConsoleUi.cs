﻿using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Robotics;

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

    public RoboticsConsoleState(Dictionary<string, CyborgControlData> cyborgs)
    {
        Cyborgs = cyborgs;
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
public partial record struct CyborgControlData
{
    /// <summary>
    /// Texture of the borg chassis.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier? ChassisSprite;

    /// <summary>
    /// Name of the borg chassis.
    /// </summary>
    [DataField(required: true)]
    public string ChassisName = string.Empty;

    /// <summary>
    /// Name of the borg's entity, including its silicon id.
    /// </summary>
    [DataField(required: true)]
    public string Name = string.Empty;

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
    /// Whether the borg can currently be disabled if the brain is installed,
    /// if on cooldown then can't queue up multiple disables.
    /// </summary>
    [DataField]
    public bool CanDisable;

    /// <summary>
    /// When this cyborg's data will be deleted.
    /// Set by the console when receiving the packet.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Timeout = TimeSpan.Zero;

    public CyborgControlData(SpriteSpecifier? chassisSprite, string chassisName, string name, float charge, int moduleCount, bool hasBrain, bool canDisable)
    {
        ChassisSprite = chassisSprite;
        ChassisName = chassisName;
        Name = name;
        Charge = charge;
        ModuleCount = moduleCount;
        HasBrain = hasBrain;
        CanDisable = canDisable;
    }
}

public static class RoboticsConsoleConstants
{
    // broadcast by cyborgs on Robotics Console frequency
    public const string NET_CYBORG_DATA = "cyborg-data";

    // sent by robotics console to cyborgs on Cyborg Control frequency
    public const string NET_DISABLE_COMMAND = "cyborg-disable";
    public const string NET_DESTROY_COMMAND = "cyborg-destroy";
}

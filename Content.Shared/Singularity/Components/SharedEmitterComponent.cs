using System.Threading;
using Content.Shared.Construction.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed class EmitterComponent : Component
{
    public CancellationTokenSource? TimerCancel;

    // whether the power switch is in "on"
    [ViewVariables] public bool IsOn;
    // Whether the power switch is on AND the machine has enough power (so is actively firing)
    [ViewVariables] public bool IsPowered;

    /// <summary>
    /// counts the number of consecutive shots fired.
    /// </summary>
    [ViewVariables]
    public int FireShotCounter;

    /// <summary>
    /// The entity that is spawned when the emitter fires.
    /// </summary>
    [DataField("boltType")]
    public string BoltType = "EmitterBolt";

    /// <summary>
    /// The current amount of power being used.
    /// </summary>
    [DataField("powerUseActive")]
    public int PowerUseActive = 600;

    /// <summary>
    /// The base amount of power that is consumed.
    /// Used in machine part rating calculations.
    /// </summary>
    [DataField("basePowerUseActive"), ViewVariables(VVAccess.ReadWrite)]
    public int BasePowerUseActive = 600;

    /// <summary>
    /// Multiplier that is applied to the basePowerUseActive
    /// to get the actual power use.
    /// </summary>
    [DataField("powerUseMultiplier")]
    public float PowerUseMultiplier = 0.75f;

    /// <summary>
    /// The machine part used to reduce the power use of the machine.
    /// </summary>
    [DataField("machinePartPowerUse", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartPowerUse = "Capacitor";

    /// <summary>
    /// The amount of shots that are fired in a single "burst"
    /// </summary>
    [DataField("fireBurstSize")]
    public int FireBurstSize = 3;

    /// <summary>
    /// The time between each shot during a burst.
    /// </summary>
    [DataField("fireInterval")]
    public TimeSpan FireInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The base amount of time between each shot during a burst.
    /// </summary>
    [DataField("baseFireInterval"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BaseFireInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The current minimum delay between bursts.
    /// </summary>
    [DataField("fireBurstDelayMin")]
    public TimeSpan FireBurstDelayMin = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The current maximum delay between bursts.
    /// </summary>
    [DataField("fireBurstDelayMax")]
    public TimeSpan FireBurstDelayMax = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The base minimum delay between shot bursts.
    /// Used for machine part rating calculations.
    /// </summary>
    [DataField("baseFireBurstDelayMin")]
    public TimeSpan BaseFireBurstDelayMin = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The base maximum delay between shot bursts.
    /// Used for machine part rating calculations.
    /// </summary>
    [DataField("baseFireBurstDelayMax")]
    public TimeSpan BaseFireBurstDelayMax = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The multiplier for the base delay between shot bursts as well as
    /// the fire interval
    /// </summary>
    [DataField("fireRateMultiplier"), ViewVariables(VVAccess.ReadWrite)]
    public float FireRateMultiplier = 0.8f;

    /// <summary>
    /// The machine part that affects burst delay.
    /// </summary>
    [DataField("machinePartFireRate", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
    public string MachinePartFireRate = "Laser";

    /// <summary>
    /// The visual state that is set when the emitter is turned on
    /// </summary>
    [DataField("onState")]
    public string? OnState = "beam";

    /// <summary>
    /// The visual state that is set when the emitter doesn't have enough power.
    /// </summary>
    [DataField("underpoweredState")]
    public string? UnderpoweredState = "underpowered";
}

[NetSerializable, Serializable]
public enum EmitterVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum EmitterVisualLayers : byte
{
    Lights
}

[NetSerializable, Serializable]
public enum EmitterVisualState
{
    On,
    Underpowered,
    Off
}

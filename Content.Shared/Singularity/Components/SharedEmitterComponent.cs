using System.Threading;
using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmitterComponent : Component
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
    [DataField("boltType", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string BoltType = "EmitterBolt";

    [DataField("selectableTypes", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
    public List<string> SelectableTypes = new();

    /// <summary>
    /// The current amount of power being used.
    /// </summary>
    [DataField("powerUseActive")]
    public int PowerUseActive = 600;

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
    /// The visual state that is set when the emitter is turned on
    /// </summary>
    [DataField("onState")]
    public string? OnState = "beam";

    /// <summary>
    /// The visual state that is set when the emitter doesn't have enough power.
    /// </summary>
    [DataField("underpoweredState")]
    public string? UnderpoweredState = "underpowered";

    /// <summary>
    /// Signal port that turns on the emitter.
    /// </summary>
    [DataField("onPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OnPort = "On";

    /// <summary>
    /// Signal port that turns off the emitter.
    /// </summary>
    [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string OffPort = "Off";

    /// <summary>
    /// Signal port that toggles the emitter on or off.
    /// </summary>
    [DataField("togglePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string TogglePort = "Toggle";

    /// <summary>
    /// Map of signal ports to entity prototype IDs of the entity that will be fired.
    /// </summary>
    [DataField("setTypePorts", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<string, SinkPortPrototype>))]
    public Dictionary<string, string> SetTypePorts = new();
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

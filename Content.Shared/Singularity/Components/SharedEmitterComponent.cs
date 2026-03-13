using System.Threading;
using Content.Shared.DeviceLinking;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    [DataField, AutoNetworkedField]
    public EntProtoId BoltType = "EmitterBolt";

    [DataField]
    public List<EntProtoId> SelectableTypes = new();

    /// <summary>
    /// The current amount of power being used.
    /// </summary>
    [DataField]
    public int PowerUseActive = 600;

    /// <summary>
    /// The amount of shots that are fired in a single "burst"
    /// </summary>
    [DataField]
    public int FireBurstSize = 3;

    /// <summary>
    /// The time between each shot during a burst.
    /// </summary>
    [DataField]
    public TimeSpan FireInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The current minimum delay between bursts.
    /// </summary>
    [DataField]
    public TimeSpan FireBurstDelayMin = TimeSpan.FromSeconds(4);

    /// <summary>
    /// The current maximum delay between bursts.
    /// </summary>
    [DataField]
    public TimeSpan FireBurstDelayMax = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The visual state that is set when the emitter is turned on
    /// </summary>
    [DataField]
    public string? OnState = "beam";

    /// <summary>
    /// The visual state that is set when the emitter doesn't have enough power.
    /// </summary>
    [DataField]
    public string? UnderpoweredState = "underpowered";

    /// <summary>
    /// Signal port that turns on the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    /// <summary>
    /// Signal port that turns off the emitter.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> OffPort = "Off";

    /// <summary>
    /// Signal port that toggles the emitter on or off.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    /// <summary>
    /// Map of signal ports to entity prototype IDs of the entity that will be fired.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SinkPortPrototype>, EntProtoId> SetTypePorts = new();

    /// <summary>
    /// The radio channel to broadcast on when something happens to this emitter
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Engineering";

    /// <summary>
    /// Whether a radio channel should be alerted if anything happens to this emitter (i.e. emitters near singularity/tesla containment)
    /// </summary>
    [DataField]
    public bool AlertRadio = false;

    /// <summary>
    /// Localized string to use when this emitter is destroyed and AlertRadio is set to true
    /// </summary>
    [DataField]
    public string LocDestroyed = "emitter-destroyed-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is deconstructed and AlertRadio is set to true
    /// </summary>
    [DataField]
    public string LocDeconstructed = "emitter-deconstructed-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is unlocked and AlertRadio is set to true
    /// </summary>
    [DataField]
    public string LocUnlocked = "emitter-unlocked-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is unpowered and AlertRadio is set to true
    /// </summary>
    [DataField]
    public string LocUnpowered = "emitter-unpowered-broadcast";
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

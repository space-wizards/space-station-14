using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Singularity.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EmitterComponent : Component
{
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
    public LocId LocDestroyed = "emitter-destroyed-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is deconstructed and AlertRadio is set to true
    /// </summary>
    [DataField]
    public LocId LocDeconstructed = "emitter-deconstructed-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is unlocked and AlertRadio is set to true
    /// </summary>
    [DataField]
    public LocId LocUnlocked = "emitter-unlocked-broadcast";

    /// <summary>
    /// Localized string to use when this emitter is unpowered and AlertRadio is set to true
    /// </summary>
    [DataField]
    public LocId LocUnpowered = "emitter-unpowered-broadcast";
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


[Serializable, NetSerializable]
public sealed class NetworkPoweredAmmoProviderToggleActiveMessage : BoundUserInterfaceMessage;

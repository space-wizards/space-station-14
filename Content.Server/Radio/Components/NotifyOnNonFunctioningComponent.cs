using Content.Server.Radio.EntitySystems;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.Components;

/// <summary>
/// Component for attempting to post radio message to chat
/// upon something destructive happens to device.
/// Can be used for singularity containment field emitters
/// or other crucial parts of infrastructure.
/// </summary>
[RegisterComponent]
[Access(typeof(NotifyOnNonFunctioningSystem))]
public sealed partial class NotifyOnNonFunctioningComponent : Component
{
    /// <summary>
    /// The radio channel to broadcast on when something happens to this device
    /// </summary>
    [DataField(required: true)]
    public ProtoId<RadioChannelPrototype> RadioChannel;

    /// <summary>
    /// Localized string to use when this device is destroyed.
    /// </summary>
    [DataField]
    public LocId? LocDestroyed;

    /// <summary>
    /// Localized string to use when this device is deconstructed.
    /// </summary>
    [DataField]
    public LocId? LocDeconstructed;

    /// <summary>
    /// Localized string to use when this device is unlocked.
    /// </summary>
    [DataField]
    public LocId? LocUnlocked;

    /// <summary>
    /// Localized string to use when this device have
    /// no power supply to keep working.
    /// </summary>
    [DataField]
    public LocId? LocUnpowered;

    /// <summary>
    /// Localized string to use when this device is turned off.
    /// </summary>
    [DataField]
    public LocId? LocTurnedOff;

    /// <summary>
    /// Localized string to use when this device is unanchored
    /// (and is most likely is not able to function).
    /// </summary>
    [DataField]
    public LocId? LocUnanchored;

    /// <summary>
    /// Marker, if power is required to send radio message.
    /// </summary>
    [DataField]
    public bool RequirePowered;
}

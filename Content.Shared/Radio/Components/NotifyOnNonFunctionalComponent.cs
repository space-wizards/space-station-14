using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NotifyOnNonFunctionalComponent : Component
{
    /// <summary>
    /// The radio channel to broadcast on when something happens to this emitter
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Engineering";

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

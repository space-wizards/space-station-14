using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Command.PanicButton;

[RegisterComponent]
public sealed partial class PanicButtonComponent : Component
{
    /// <summary>
    ///     The radio channel the message will be sent to
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Security";

    /// <summary>
    ///     The message that will be sent when the trigger is triggered
    /// </summary>
    [DataField]
    public LocId RadioMessage = "comp-panic-button-message";

    /// <summary>
    ///     Should the location of the sender of the message be indicated relative to the nearest station beacon
    /// </summary>
    [DataField]
    public bool SpecifyLocation = true;
}

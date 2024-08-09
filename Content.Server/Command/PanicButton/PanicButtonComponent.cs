using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Command.PanicButton;

/// <summary>
///     The component is used for items with a battery.
///     When using a thing with this component in your hands, a message is sent to a certain radio channel.
/// </summary>
[RegisterComponent]
public sealed partial class PanicButtonComponent : Component
{
    /// <summary>
    ///     The radio channel the message will be sent to.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Security";

    /// <summary>
    ///     The message that will be sent when the item will be used
    /// </summary>
    [DataField]
    public LocId RadioMessage = "comp-panic-button-message";

    /// <summary>
    ///     Should the location of the sender of the message be indicated relative to the nearest station beacon.
    /// </summary>
    [DataField]
    public bool SpecifyLocation = true;
}

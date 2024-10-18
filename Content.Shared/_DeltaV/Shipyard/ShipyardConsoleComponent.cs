using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DeltaV.Shipyard;

/// <summary>
/// Component for the shipyard console.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedShipyardConsoleSystem))]
public sealed partial class ShipyardConsoleComponent : Component
{
    /// <summary>
    /// Sound played when the ship can't be bought for any reason.
    /// </summary>
    [DataField]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    /// <summary>
    /// Sound played when a ship is purchased.
    /// </summary>
    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    /// <summary>
    /// Radio channel to send the purchase announcement to.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Command";
}

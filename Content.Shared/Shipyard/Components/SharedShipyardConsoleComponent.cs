using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Shipyard.Components;

[RegisterComponent, NetworkedComponent]
public sealed class ShipyardConsoleComponent : Component
{
    [DataField("soundError")]
    public SoundSpecifier ErrorSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField("soundConfirm")]
    public SoundSpecifier ConfirmSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField("shipyardChannel")]
    public string ShipyardChannel = "Command";
}

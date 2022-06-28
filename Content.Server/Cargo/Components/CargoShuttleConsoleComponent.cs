using Content.Shared.Sound;

namespace Content.Server.Cargo.Components;

[RegisterComponent]
public sealed class CargoShuttleConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDeny")]
    public SoundSpecifier DenySound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_two.ogg");
}

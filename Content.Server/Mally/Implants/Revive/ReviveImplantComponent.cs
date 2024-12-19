namespace Content.Server.Mally.Implants.Revive;
using Robust.Shared.Audio;

[RegisterComponent]
public sealed partial class ReviveImplantComponent : Component
{
    [DataField]
    public float InjectTime = 4f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public SoundSpecifier ImplantedSound = new SoundPathSpecifier("/Audio/Mally/sound_weapons_circsawhit.ogg");
}

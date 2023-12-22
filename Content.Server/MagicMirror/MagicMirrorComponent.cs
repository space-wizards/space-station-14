using Content.Shared.Humanoid;
using Robust.Shared.Audio;

namespace Content.Server.MagicMirror;

[RegisterComponent]
public sealed partial class MagicMirrorComponent : Component
{
    public Entity<HumanoidAppearanceComponent>? Target;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AddSlotTime = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float RemoveSlotTime = 2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SelectSlotTime = 3f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChangeSlotTime = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier ChangeHairSound = new SoundPathSpecifier("/Audio/Items/scissors.ogg");
}

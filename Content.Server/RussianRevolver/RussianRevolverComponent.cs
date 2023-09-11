using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.RussianRevolver;

[RegisterComponent]
[Access]
public sealed partial class RussianRevolverComponent : Component
{
    [DataField("damage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier RussianRevolverDamage = default!;

    [DataField("soundWin"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundWin = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    [DataField("soundLose"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? SoundLose = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/revolver.ogg");
}

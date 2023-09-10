using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server.RussianRevolver;

[RegisterComponent]
public sealed partial class RussianRevolverComponent : Component
{
    [DataField("russianRevolverDamage", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier RussianRevolverDamage = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundWin")]
    public SoundSpecifier? SoundWin = new SoundPathSpecifier("/Audio/Weapons/Guns/Empty/empty.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundLose")]
    public SoundSpecifier? SoundLose = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/revolver.ogg");
}

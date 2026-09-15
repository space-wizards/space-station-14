using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class StaminaDamageOnHitComponent : StaminaDamageComponent
{
    [DataField]
    public SoundSpecifier? Sound;
}

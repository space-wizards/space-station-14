using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

[RegisterComponent]
public sealed partial class BoxingComponent : Component
{
    [DataField]
    public SoundSpecifier? Sound;
}

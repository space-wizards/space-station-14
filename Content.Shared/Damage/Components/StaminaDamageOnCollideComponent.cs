using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when colliding with an entity.
/// </summary>
[RegisterComponent]
public sealed partial class StaminaDamageOnCollideComponent : StaminaDamageComponent
{
    [DataField]
    public SoundSpecifier? Sound;
}

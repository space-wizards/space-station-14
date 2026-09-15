using Robust.Shared.Audio;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Applies stamina damage when colliding with an entity.
/// </summary>
public sealed partial class StaminaDamageOnCollideComponent : StaminaDamageComponent
{
    [DataField]
    public SoundSpecifier? Sound;
}

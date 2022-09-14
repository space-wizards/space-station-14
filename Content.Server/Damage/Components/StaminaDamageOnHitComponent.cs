using Robust.Shared.Audio;

namespace Content.Server.Damage.Components;

[RegisterComponent]
public sealed class StaminaDamageOnHitComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public float Damage = 30f;

    /// <summary>
    /// Play a sound when this knocks down an entity.
    /// </summary>
    [DataField("knockdownSound")]
    public SoundSpecifier? KnockdownSound;
}

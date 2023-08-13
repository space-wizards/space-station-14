using Content.Shared.Damage;

namespace Content.Server.Flash.Components;

/// <summary>
/// This is used for damaging an entity when they get flashed.
/// </summary>
[RegisterComponent]
public sealed class DamageOnFlashedComponent : Component
{
    /// <summary>
    /// The damage done when flashed.
    /// </summary>
    [DataField("damage", required: true)]
    public DamageSpecifier? Damage;
}

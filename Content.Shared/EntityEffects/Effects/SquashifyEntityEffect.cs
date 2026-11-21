using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Makes this entity burst once it takes enough damage, spilling one of its solutions and destroying itself.
/// The entity needs to be damageable for this to do anything.
/// Adds to an existing destructible setup instead of replacing it.
/// </summary>
/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Squashify : EntityEffectBase<Squashify>
{
    /// <summary>
    /// Total damage at which the entity bursts.
    /// </summary>
    [DataField]
    public FixedPoint2 Damage = 1;

    /// <summary>
    /// The solution spilled when the entity bursts.
    /// </summary>
    [DataField]
    public string Solution = "food";

    /// <summary>
    /// Sound played when the entity bursts.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}

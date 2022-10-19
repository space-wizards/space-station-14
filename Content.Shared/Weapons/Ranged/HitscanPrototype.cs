using Content.Shared.Damage;
using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

[Prototype("hitscan")]
public readonly record struct HitscanPrototype : IPrototype, IShootable
{
    [ViewVariables]
    [IdDataFieldAttribute]
    public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("staminaDamage")]
    public readonly float StaminaDamage;

    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public readonly DamageSpecifier? Damage;

    [ViewVariables(VVAccess.ReadOnly), DataField("muzzleFlash")]
    public readonly SpriteSpecifier? MuzzleFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("travelFlash")]
    public readonly SpriteSpecifier? TravelFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("impactFlash")]
    public readonly SpriteSpecifier? ImpactFlash;

    [ViewVariables, DataField("collisionMask")]
    public readonly int CollisionMask = (int) CollisionGroup.Opaque;

    /// <summary>
    /// Sound that plays upon the thing being hit.
    /// </summary>
    [ViewVariables] [DataField("sound")] public readonly SoundSpecifier? Sound;

    /// <summary>
    /// Force the hitscan sound to play rather than potentially playing the entity's sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("forceSound")]
    public readonly bool ForceSound;

    /// <summary>
    /// Try not to set this too high.
    /// </summary>
    [ViewVariables, DataField("maxLength")]
    public readonly float MaxLength = 20f;
}

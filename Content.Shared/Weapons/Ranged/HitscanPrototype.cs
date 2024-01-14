using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

[Prototype]
public sealed partial class HitscanPrototype : IPrototype, IShootable
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float StaminaDamage;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? Damage;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public SpriteSpecifier? MuzzleFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public SpriteSpecifier? TravelFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public SpriteSpecifier? ImpactFlash;

    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.Opaque;

    /// <summary>
    /// What we count as for reflection.
    /// </summary>
    [DataField] public ReflectType Reflective = ReflectType.Energy;

    /// <summary>
    /// Sound that plays upon the thing being hit.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Force the hitscan sound to play rather than potentially playing the entity's sound.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool ForceSound;

    /// <summary>
    /// Try not to set this too high.
    /// </summary>
    [DataField]
    public float MaxLength = 20f;

    /// <summary>
    /// Is the hitscan allowed to penetrate.
    /// </summary>
    [DataField]
    public bool CanPenetrate;

    /// <summary>
    /// The CollisionLayer, up to and including, the hitscan is allowed to penetrate.
    /// </summary>
    /// <remarks>
    /// The hitscan can penetrate everything if this value is not set.
    /// </remarks>
    [DataField]
    public CollisionGroup? PenetrationLayer;

    /// <summary>
    /// Is the hitscan blocked by entities with a certain radiation resistance.
    /// </summary>
    /// <remarks>
    /// Entities with a radiation resistance higher than the hitscan's PenetrationPower will not be penetrated.
    /// </remarks>
    [DataField]
    public bool BlockedByRadiationResistance;

    /// <summary>
    /// Decides how many entities can be penetrated if BlockedByRadiationResistance is false.
    /// Decides how high of a radiation resistance can be penetrated if BlockedByRadiationResistance is true.
    /// </summary>
    [DataField]
    public int PenetrationPower = 4;
}

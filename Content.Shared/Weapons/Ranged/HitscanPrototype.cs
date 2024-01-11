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
    public int CollisionMask = (int) CollisionGroup.Opaque;

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
    /// Decides if the hitscan can penetrate living entities.
    /// </summary>
    [DataField]
    public bool CanPenetrateMobs;

    /// <summary>
    /// Decides if the hitscan can penetrate walls.
    /// </summary>
    [DataField]
    public bool CanPenetrateWall;

    /// <summary>
    /// Decides how high of a radiation resistance can be penetrated,
    /// </summary>
    [DataField]
    public int PenetrationPower = 5;
}

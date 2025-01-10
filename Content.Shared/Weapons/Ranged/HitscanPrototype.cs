using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged;

[Prototype("hitscan")]
public sealed partial class HitscanPrototype : IPrototype, IShootable, IInheritingPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    // 🌟Starlight🌟
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<HitscanPrototype>))]
    public string[]? Parents { get; private set; }
    // 🌟Starlight🌟
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    // 🌟Starlight🌟
    [ViewVariables(VVAccess.ReadWrite), DataField("staminaDamage")]
    public float StaminaDamage;

    // 🌟Starlight🌟
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Ignite = false;

    // 🌟Starlight🌟
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool IgnoreResistances = false;

    // 🌟Starlight🌟
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int Temperature = 700;

    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier? Damage;

    [ViewVariables(VVAccess.ReadOnly), DataField("muzzleFlash")]
    public SpriteSpecifier? MuzzleFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("travelFlash")]
    public SpriteSpecifier? TravelFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("impactFlash")]
    public SpriteSpecifier? ImpactFlash;

    [ViewVariables(VVAccess.ReadOnly), DataField("bullet")]
    public SpriteSpecifier? Bullet;

    [DataField("collisionMask")]
    public int CollisionMask = (int) CollisionGroup.Opaque;
    
    [DataField("igniteOnCollision")]
    public bool ignite = false;

    /// <summary>
    /// What we count as for reflection.
    /// </summary>
    [DataField("reflective")] public ReflectType Reflective = ReflectType.Energy;

    /// <summary>
    /// Sound that plays upon the thing being hit.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Force the hitscan sound to play rather than potentially playing the entity's sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("forceSound")]
    public bool ForceSound;

    /// <summary>
    /// Try not to set this too high.
    /// </summary>
    [DataField("maxLength")]
    public float MaxLength = 20f;

    /// <summary>
    /// How much the ammo spreads when shot, in degrees. Does nothing if count is 0.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.FromDegrees(5);

    [DataField]
    public int Count = 1;
}

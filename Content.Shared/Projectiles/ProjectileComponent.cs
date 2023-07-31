using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("impactEffect", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ImpactEffect;

    /// <summary>
    /// User that shot this projectile.
    /// </summary>
    [DataField("shooter"), AutoNetworkedField] public EntityUid Shooter;

    /// <summary>
    /// Weapon used to shoot.
    /// </summary>
    [DataField("weapon"), AutoNetworkedField]
    public EntityUid Weapon;

    [DataField("ignoreShooter"), AutoNetworkedField]
    public bool IgnoreShooter = true;

    [DataField("damage", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("deleteOnCollide")]
    public bool DeleteOnCollide = true;

    /// <summary>
    /// Can the projectile penetrate mobs.
    /// </summary>
    [DataField("canPenetrate")]
    public bool CanPenetrate = false;
    /// <summary>
    /// Can the projectile penetrate walls.
    /// </summary>
    [DataField("canPenetrateWall")]
    public bool CanPenetrateWall = false;
    /// <summary>
    /// The amount of entities the projectile can pierce.
    /// </summary>
    [DataField("penetrationStrength")]
    public float PenetrationStrength = 0f;
    /// <summary>
    /// The amount of damage that is lost every time the bullet pierces an entity.
    /// </summary>
    [DataField("penetrationDamageFalloffMultiplier")]
    public float PenetrationDamageFalloffMultiplier = 0.5f;
    /// <summary>
    /// Checks if the weapon modifier has been added.
    /// </summary>
    [DataField("weaponModifierAdded")]
    public bool DamageModifierAdded = false;
    /// <summary>
    /// Checks if the penetrationModifier from the gun has been added.
    /// </summary>
    [DataField("penetrationModifierAdded")]
    public bool PenetrationModifierAdded = false;

    // Get that juicy FPS hit sound
    [DataField("soundHit")] public SoundSpecifier? SoundHit;

    [DataField("soundForce")]
    public bool ForceSound = false;

    public bool DamagedEntity;
}

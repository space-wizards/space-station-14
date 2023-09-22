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
    [DataField("shooter"), AutoNetworkedField]
    public EntityUid? Shooter;

    /// <summary>
    /// Weapon used to shoot.
    /// </summary>
    [DataField("weapon"), AutoNetworkedField]
    public EntityUid? Weapon;

    [DataField("ignoreShooter"), AutoNetworkedField]
    public bool IgnoreShooter = true;

    [DataField("damage", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    [DataField("deleteOnCollide")]
    public bool DeleteOnCollide = true;

    [DataField("ignoreResistances")]
    public bool IgnoreResistances = false;

    // Get that juicy FPS hit sound
    [DataField("soundHit")] public SoundSpecifier? SoundHit;

    [DataField("soundForce")]
    public bool ForceSound = false;

    /// <summary>
    ///     Whether this projectile will only collide with entities if it was shot from a gun (if <see cref="Weapon"/> is not null)
    /// </summary>
    [DataField("onlyCollideWhenShot")]
    public bool OnlyCollideWhenShot = false;

    /// <summary>
    ///     Whether this projectile has already damaged an entity.
    /// </summary>
    public bool DamagedEntity;
}

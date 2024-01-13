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
    ///     User that shot this projectile.
    /// </summary>
    [DataField("shooter"), AutoNetworkedField]
    public EntityUid? Shooter;

    /// <summary>
    ///     Weapon used to shoot.
    /// </summary>
    [DataField("weapon"), AutoNetworkedField]
    public EntityUid? Weapon;

    /// <summary>
    ///     The projectile spawns inside the shooter most of the time, this prevents entities from shooting themselves.
    /// </summary>
    [DataField("ignoreShooter"), AutoNetworkedField]
    public bool IgnoreShooter = true;

    /// <summary>
    ///     The amount of damage the projectile will do.
    /// </summary>
    [DataField("damage", required: true)] [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = new();

    /// <summary>
    ///     If the target should be deleted on collision.
    /// </summary>
    [DataField("deleteOnCollide")]
    public bool DeleteOnCollide = true;

    /// <summary>
    ///     If the projectile should keep the ability to deal damage after colliding.
    /// </summary>
    [DataField]
    public bool DamageAfterCollide = false;

    /// <summary>
    ///     Penetrate the target only if it has the MobStateComponent.
    /// </summary>
    /// <remarks>
    ///     DeleteOnCollide needs to be false for this to work.
    /// </remarks>
    [DataField]
    public bool CanOnlyPenetrateMobs = false;

    /// <summary>
    ///     Ignore all damage resistances the target has.
    /// </summary>
    [DataField]
    public bool IgnoreResistances = false;

    // Get that juicy FPS hit sound
    [DataField] public SoundSpecifier? SoundHit;

    /// <summary>
    ///     Force the projectiles sound to play rather than potentially playing the entity's sound.
    /// </summary>
    [DataField]
    public bool ForceSound = false;

    /// <summary>
    ///     Whether this projectile will only collide with entities if it was shot from a gun (if <see cref="Weapon"/> is not null)
    /// </summary>
    [DataField]
    public bool OnlyCollideWhenShot = false;

    /// <summary>
    ///     Whether this projectile has already damaged an entity.
    /// </summary>
    [DataField]
    public bool DamagedEntity;
}

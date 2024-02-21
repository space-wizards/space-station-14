using Content.Shared.FixedPoint;
using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent]
public sealed partial class CanPenetrateComponent : Component
{
    /// <summary>
    ///     Should the projectile keep the ability to deal damage after colliding.
    /// </summary>
    [DataField]
    public bool DamageAfterCollide = true;

    /// <summary>
    ///     The CollisionLayer, up to and including the one set, the projectile is allowed to penetrate.
    /// </summary>
    ///<remarks>
    ///     Can penetrate everything if this value is not set.
    /// </remarks>
    [DataField]
    public CollisionGroup? PenetrationLayer;

    /// <summary>
    ///     How many times the projectile is allowed to deal damage.
    /// </summary>
    /// <remarks>
    ///     Can deal damage on every collision if this value is not set.
    /// </remarks>
    [DataField]
    public float? PenetrationPower;

    /// <summary>
    ///     Modifies the damage of a projectile after it has penetrated an entity.
    /// </summary>
    /// <remarks>
    ///     Won't modify the projectile's damage if this value is not set.
    /// </remarks>
    [DataField]
    public FixedPoint2? DamageModifier;
}

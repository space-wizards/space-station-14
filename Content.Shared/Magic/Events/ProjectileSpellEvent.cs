using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class ProjectileSpellEvent : WorldTargetActionEvent
{
    /// <summary>
    /// What entity should be spawned.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype;

    /// <summary>
    /// How fast the projectile should travel
    /// </summary>
    [DataField]
    public float ProjectileSpeed = 25f;

    /// <summary>
    /// A coefficient to adjust velocity by when shooting the opposite way you're moving.
    /// </summary>
    /// <remarks>
    /// Useful for slow projectiles to prevent them from feeling sluggish.
    /// A value of 0.5 increases projectile speed by 50% if shot directly behind you.
    /// </remarks>
    [DataField]
    public float RearVelocityCompensation;
}

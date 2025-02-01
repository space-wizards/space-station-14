using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;
/// <summary>
/// Grenades that, when triggered, explode into projectiles
/// </summary>
[RegisterComponent, Access(typeof(ProjectileGrenadeSystem))]
public sealed partial class ProjectileGrenadeComponent : Component
{
    public Container Container = default!;

    /// <summary>
    /// The kind of projectile that the prototype is filled with.
    /// </summary>
    [DataField]
    public EntProtoId? FillPrototype;

    /// <summary>
    ///     If we have a pre-fill how many more can we spawn.
    /// </summary>
    public int UnspawnedCount;

    /// <summary>
    ///     Total amount of projectiles
    /// </summary>
    [DataField]
    public int Capacity = 3;

    /// <summary>
    ///     Should the angle of the projectiles be uneven?
    /// </summary>
    [DataField]
    public bool RandomAngle = false;

    /// <summary>
    /// The minimum speed the projectiles may come out at
    /// </summary>
    [DataField]
    public float MinVelocity = 2f;

    /// <summary>
    /// The maximum speed the projectiles may come out at
    /// </summary>
    [DataField]
    public float MaxVelocity = 6f;
}

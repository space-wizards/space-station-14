using Content.Server.Explosion.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Explosion.Components;

[RegisterComponent, Access(typeof(FragmentingGrenadeSystem))]

/// <summary>
/// Use this component if the grenade splits into shootable projectiles
/// </summary>
public sealed partial class FragmentingGrenadeComponent : Component
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
    ///     The speed the projectiles are shot at
    /// </summary>
    [DataField]
    public float Velocity = 1.5f;
}

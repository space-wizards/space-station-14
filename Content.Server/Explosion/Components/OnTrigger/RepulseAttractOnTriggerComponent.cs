using Content.Shared.Physics;
using Content.Shared.Whitelist;

namespace Content.Server.Explosion.Components.OnTrigger;

/// <summary>
/// Generates a gravity pulse/repulse using the RepulseAttractComponent when the entity is triggered
/// </summary>
[RegisterComponent]
public sealed partial class RepulseAttractOnTriggerComponent : Component
{
    /// <summary>
    ///     How fast should the Repulsion/Attraction be?
    ///     A positive value will repulse objects, a negative value will attract
    /// </summary>
    [DataField]
    public float Speed = 0f;

    /// <summary>
    ///     How close do the entities need to be?
    /// </summary>
    [DataField]
    public float Range = 0f;

    /// <summary>
    ///     What kind of entities should this effect apply to?
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    ///     What collision layers should be excluded?
    ///     The default excludes ghost mobs, revenants, the AI camera etc.
    /// </summary>
    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.GhostImpassable;
}

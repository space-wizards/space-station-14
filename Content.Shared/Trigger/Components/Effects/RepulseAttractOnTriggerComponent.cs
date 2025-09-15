using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Trigger.Components.Effects;

/// <summary>
/// Generates a gravity pulse/repulse using the RepulseAttractComponent around the entity when triggered.
/// If TargetUser is true their location will be used instead.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RepulseAttractOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// How fast should the Repulsion/Attraction be?
    /// A positive value will repulse objects, a negative value will attract.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Speed = 5.0f;

    /// <summary>
    /// How close do the entities need to be?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 5.0f;

    /// <summary>
    /// What kind of entities should this effect apply to?
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// What collision layers should be excluded?
    /// The default excludes ghost mobs, revenants, the AI camera etc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public CollisionGroup CollisionMask = CollisionGroup.GhostImpassable;
}

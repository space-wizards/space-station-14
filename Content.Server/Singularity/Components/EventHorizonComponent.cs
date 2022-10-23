using Content.Shared.Singularity.Components;
using Content.Server.Singularity.EntitySystems;

namespace Content.Server.Singularity.Components;

/// <summary>
/// The server-side version of <see cref="SharedEventHorizonComponent">.
/// Primarily managed by <see cref="EventHorizonSystem">.
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedEventHorizonComponent))]
public sealed class EventHorizonComponent : SharedEventHorizonComponent
{
    /// <summary>
    /// Whether the entity this event horizon is attached to is being consumed by another event horizon.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool BeingConsumedByAnotherEventHorizon = false;

    /// <summary>
    /// The amount of time between the moments when the event horizon consumes everything it overlaps in seconds.
    /// </summary>
    [DataField("consumePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ConsumePeriod = 0.5f;

    /// <summary>
    /// The amount of time that has passed since the last moment when the event horizon consumed eveything it overlapped in seconds.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(friends:typeof(EventHorizonSystem), Other=AccessPermissions.Read, Self=AccessPermissions.Read)]
    public float _timeSinceLastConsumeWave = float.PositiveInfinity;
}

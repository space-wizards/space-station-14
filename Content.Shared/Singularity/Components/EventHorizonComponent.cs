using Robust.Shared.GameStates;
using Content.Shared.Singularity.EntitySystems;

namespace Content.Shared.Singularity.Components;

/// <summary>
/// A component that makes the associated entity destroy other within some distance of itself.
/// Also makes the associated entity destroy other entities upon contact.
/// Primarily managed by <see cref="SharedEventHorizonSystem"/> and its server/client versions.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class EventHorizonComponent : Component
{
    /// <summary>
    /// The radius of the event horizon within which it will destroy all entities and tiles.
    /// If < 0.0 this behavior will not be active.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetRadius"/>.
    /// </summary>
    [DataField("radius")]
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public float Radius;

    /// <summary>
    /// Whether the event horizon can consume/destroy the devices built to contain it.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetCanBreachContainment"/>.
    /// </summary>
    [DataField("canBreachContainment")]
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public bool CanBreachContainment = false;

    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetHorizonFixtureId"/>.
    /// </summary>
    [DataField("horizonFixtureId")]
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public string? HorizonFixtureId = "EventHorizon";

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
    [Access(friends:typeof(SharedEventHorizonSystem))]
    public float TimeSinceLastConsumeWave = float.PositiveInfinity;
}

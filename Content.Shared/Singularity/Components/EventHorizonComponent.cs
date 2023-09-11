using Robust.Shared.GameStates;
using Content.Shared.Singularity.EntitySystems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Singularity.Components;

/// <summary>
/// A component that makes the associated entity destroy other within some distance of itself.
/// Also makes the associated entity destroy other entities upon contact.
/// Primarily managed by <see cref="SharedEventHorizonSystem"/> and its server/client versions.
/// </summary>
[Access(friends: typeof(SharedEventHorizonSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class EventHorizonComponent : Component
{
    /// <summary>
    /// The radius of the event horizon within which it will destroy all entities and tiles.
    /// If &lt; 0.0 this behavior will not be active.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetRadius"/>.
    /// </summary>
    [DataField("radius")]
    public float Radius;

    /// <summary>
    /// Whether the event horizon can consume/destroy the devices built to contain it.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetCanBreachContainment"/>.
    /// </summary>
    [DataField("canBreachContainment")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CanBreachContainment = false;

    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetHorizonFixtureId"/>.
    /// </summary>
    [DataField("consumerFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ConsumerFixtureId = "EventHorizonConsumer";

    /// <summary>
    /// The ID of the fixture used to detect if the event horizon has collided with any physics objects.
    /// Can be set to null, in which case no such fixture is used.
    /// If you want to set this go through <see cref="SharedEventHorizonSystem.SetHorizonFixtureId"/>.
    /// </summary>
    [DataField("colliderFixtureId")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ColliderFixtureId = "EventHorizonCollider";

    /// <summary>
    /// Whether the entity this event horizon is attached to is being consumed by another event horizon.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool BeingConsumedByAnotherEventHorizon = false;

    #region Update Timing

    /// <summary>
    /// The amount of time that should elapse between this event horizon consuming everything it overlaps with.
    /// </summary>
    [DataField("consumePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TargetConsumePeriod = TimeSpan.FromSeconds(0.5);

    /// <summary>
    /// The next time at which this consumed everything it overlapped with.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), DataField("nextConsumeWaveTime", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextConsumeWaveTime;

    #endregion Update Timing
}

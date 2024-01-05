
using Content.Server.Physics.Controllers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Physics.Components;

/// <summary>
/// A component which makes its entity chasing entity with selected component.
/// </summary>
[RegisterComponent, Access(typeof(ChasingWalkSystem))]
public sealed partial class ChasingWalkComponent : Component
{
    /// <summary>
    /// The next moment in time when the entity is pushed toward its goal
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextImpulseTime;

    /// <summary>
    /// Push-to-target frequency.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ImpulseInterval = 2f;

    /// <summary>
    /// The minimum speed at which this entity will move.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinSpeed = 1.5f;

    /// <summary>
    /// The maximum speed at which this entity will move.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSpeed = 3f;

    /// <summary>
    /// The current speed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Speed;

    /// <summary>
    /// The minimum time interval in which an object can change its motion target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChangeVectorMinInterval = 5f;

    /// <summary>
    /// The maximum time interval in which an object can change its motion target.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChangeVectorMaxInterval = 25f;

    /// <summary>
    /// The next change of direction time.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextChangeVectorTime;

    /// <summary>
    /// The component that the entity is chasing
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry ChasingComponent = default!;

    /// <summary>
    /// The maximum radius in which the entity chooses the target component to follow
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxChaseRadius = 25;

    /// <summary>
    /// The entity uid, chasing by the component owner
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ChasingEntity;
}

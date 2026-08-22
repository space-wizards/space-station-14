using Content.Server.Administration.Verbs.Operations;
using Content.Server.Physics.Controllers;
using Content.Server.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Physics.Components;

/// <summary>
/// A component which makes its entity chasing entity with selected component.
/// </summary>
[RegisterComponent, Access(typeof(ChasingWalkSystem), typeof(AdminOperationSystem), typeof(GunSystem)), AutoGenerateComponentPause]
public sealed partial class ChasingWalkComponent : Component
{
    /// <summary>
    /// The next moment in time when the entity is pushed toward its goal
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoPausedField]
    public TimeSpan NextImpulseTime;

    /// <summary>
    /// Push-to-target frequency.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ImpulseInterval = 2f;

    /// <summary>
    /// The max angle the entity can turn each impulse
    /// </summary>
    [DataField]
    public Angle MaxAngleVectorChangePerImpulse = Angle.FromDegrees(180);

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
    /// If the entity should stop moving if they are already on top of the target
    /// </summary>
    [DataField]
    public bool StopAtTarget = true;

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
    [AutoPausedField]
    public TimeSpan NextChangeVectorTime;

    /// <summary>
    /// List of components used to select a target to chase.
    /// </summary>
    [DataField]
    public ComponentRegistry ChasingComponent = [];

    /// <summary>
    /// The maximum radius in which the entity chooses the target component to follow
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxChaseRadius = 25;

    /// <summary>
    /// The entity uid that is being chased.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? ChasingEntity;

    /// <summary>
    /// whether the entity should point in the direction its moving
    /// </summary>
    [DataField]
    public bool RotateWithImpulse;

    /// <summary>
    /// Sprite rotation offset.
    /// </summary>
    [DataField]
    public Angle RotationAngleOffset = Angle.Zero;
}

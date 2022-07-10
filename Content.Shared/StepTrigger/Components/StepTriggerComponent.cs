using Content.Shared.StepTrigger.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StepTrigger.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(StepTriggerSystem))]
public sealed class StepTriggerComponent : Component
{
    /// <summary>
    ///     List of entities that are currently colliding with the entity.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Colliding = new();

    /// <summary>
    ///     The list of entities that are standing on this entity,
    /// which shouldn't be able to trigger it again until stepping off.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> CurrentlySteppedOn = new();

    /// <summary>
    ///     Whether or not this component will currently try to trigger for entities.
    /// </summary>
    [DataField("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    ///     Ratio of shape intersection for a trigger to occur.
    /// </summary>
    [DataField("intersectRatio")]
    public float IntersectRatio { get; set; } = 0.3f;

    /// <summary>
    ///     Entities will only be triggered if their speed exceeds this limit.
    /// </summary>
    [DataField("requiredTriggeredSpeed")]
    public float RequiredTriggerSpeed { get; set; } = 3.5f;
}

[RegisterComponent]
[Access(typeof(StepTriggerSystem))]
public sealed class StepTriggerActiveComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class StepTriggerComponentState : ComponentState
{
    public float IntersectRatio { get; }
    public float RequiredTriggerSpeed { get; }
    public readonly HashSet<EntityUid> CurrentlySteppedOn;
    public readonly HashSet<EntityUid> Colliding;
    public readonly bool Active;

    public StepTriggerComponentState(float intersectRatio, HashSet<EntityUid> currentlySteppedOn, HashSet<EntityUid> colliding, float requiredTriggerSpeed, bool active)
    {
        IntersectRatio = intersectRatio;
        CurrentlySteppedOn = currentlySteppedOn;
        RequiredTriggerSpeed = requiredTriggerSpeed;
        Active = active;
        Colliding = colliding;
    }
}


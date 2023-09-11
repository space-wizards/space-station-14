using Content.Shared.StepTrigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.StepTrigger.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(StepTriggerSystem))]
public sealed partial class StepTriggerComponent : Component
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
    public bool Active = true;

    /// <summary>
    ///     Ratio of shape intersection for a trigger to occur.
    /// </summary>
    [DataField("intersectRatio")]
    public float IntersectRatio = 0.3f;

    /// <summary>
    ///     Entities will only be triggered if their speed exceeds this limit.
    /// </summary>
    [DataField("requiredTriggeredSpeed")]
    public float RequiredTriggerSpeed = 3.5f;

    /// <summary>
    ///     If any entities occupy the blacklist on the same tile then steptrigger won't work.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     If this is true, steptrigger will still occur on entities that are in air / weightless. They do not
    ///     by default.
    /// </summary>
    [DataField("ignoreWeightless")]
    public bool IgnoreWeightless = false;
}

[RegisterComponent]
[Access(typeof(StepTriggerSystem))]
public sealed partial class StepTriggerActiveComponent : Component
{

}

[Serializable, NetSerializable]
public sealed class StepTriggerComponentState : ComponentState
{
    public float IntersectRatio { get; }
    public float RequiredTriggerSpeed { get; }
    public readonly HashSet<NetEntity> CurrentlySteppedOn;
    public readonly HashSet<NetEntity> Colliding;
    public readonly bool Active;

    public StepTriggerComponentState(float intersectRatio, HashSet<NetEntity> currentlySteppedOn, HashSet<NetEntity> colliding, float requiredTriggerSpeed, bool active)
    {
        IntersectRatio = intersectRatio;
        CurrentlySteppedOn = currentlySteppedOn;
        RequiredTriggerSpeed = requiredTriggerSpeed;
        Active = active;
        Colliding = colliding;
    }
}


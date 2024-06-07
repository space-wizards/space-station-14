using Content.Shared.Fluids;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StepTriggerSystem), typeof(SharedPuddleSystem))]
public sealed partial class StepTriggerComponent : Component
{
    /// <summary>
    ///     List of entities that are currently colliding with the entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Colliding = new();

    /// <summary>
    ///     Whether or not this component will currently try to trigger for entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    ///     Ratio of shape intersection for a trigger to occur.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float IntersectRatio = 0.3f;

    /// <summary>
    ///     Entities will only be triggered if their speed exceeds this limit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RequiredTriggeredSpeed = 3.5f;

    /// <summary>
    ///     If any entities occupy the blacklist on the same tile then steptrigger won't work.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    ///     If this is true, steptrigger will still occur on entities that are in air / weightless. They do not
    ///     by default.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IgnoreWeightless;

    /// <summary>
    ///     Does this have separate "StepOn" and "StepOff" triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HasStepOnOffTriggers = false;

    /// <summary>
    ///     The list of entities that are standing on this entity,
    ///     which shouldn't be able to trigger it again until stepping off.
    ///     Requires <see cref="HasStepOnOrOffTriggers"/> to be true to contain anything
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> CurrentlySteppedOn = new();

    /// <summary>
    ///     Entity will only trigger the step on trigger if speed exceeds this limit
    ///     Useful if you want to know what is currently stepping on the step trigger
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RequiredStepOnTriggeredSpeed = 0f;

    /// <summary>
    ///     Entity will only trigger the step off trigger if speed exceeds this limit
    ///     For example, this could be used to allow someone to
    ///     very slowly step off of a land mine if they stepped on it quite fast
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RequiredStepOffTriggeredSpeed = 3.5f;
}

[RegisterComponent]
[Access(typeof(StepTriggerSystem))]
public sealed partial class StepTriggerActiveComponent : Component
{

}

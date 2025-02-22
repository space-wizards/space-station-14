using Content.Shared.StepTrigger.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.StepTrigger.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(StepTriggerSystem))]
public sealed partial class StepTriggerComponent : Component
{
    public const float DefaultRequiredTriggeredSpeed = 3.5f;
    /// <summary>
    ///     List of entities that are currently colliding with the entity.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> Colliding = new();

    /// <summary>
    ///     The list of entities that are standing on this entity,
    /// which shouldn't be able to trigger it again until stepping off.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<EntityUid> CurrentlySteppedOn = new();

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
    public float RequiredTriggeredSpeed = DefaultRequiredTriggeredSpeed;

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
    /// Does this have separate "StepOn" and "StepOff" triggers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool StepOn = false;
}

[RegisterComponent]
[Access(typeof(StepTriggerSystem))]
public sealed partial class StepTriggerActiveComponent : Component
{

}

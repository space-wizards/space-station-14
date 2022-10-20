using Content.Shared.Interaction;
using Content.Shared.Whitelist;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions.ActionTypes;

[Serializable, NetSerializable]
public abstract class TargetedAction : ActionType
{
    /// <summary>
    ///     For entity- or map-targeting actions, if this is true the action will remain selected after it is used, so
    ///     it can be continuously re-used. If this is false, the action will be deselected after one use.
    /// </summary>
    [DataField("repeat")] public bool Repeat;

    /// <summary>
    ///     For  entity- or map-targeting action, determines whether the action is deselected if the user doesn't click a valid target.
    /// </summary>
    [DataField("deselectOnMiss")] public bool DeselectOnMiss;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot actually access the target
    ///     (unobstructed, in inventory, in backpack, etc). Some spells or abilities may want to disable this and
    ///     implement their own checks.
    /// </summary>
    /// <remarks>
    ///     Even if this is false, the <see cref="Range"/> will still be checked.
    /// </remarks>
    [DataField("checkCanAccess")] public bool CheckCanAccess = true;

    [DataField("range")] public float Range = SharedInteractionSystem.InteractionRange;

    /// <summary>
    ///     If the target is invalid, this bool determines whether the left-click will default to performing a standard-interaction
    /// </summary>
    /// <remarks>
    ///     Interactions will still be blocked if the target-validation generates a pop-up
    /// </remarks>
    [DataField("interactOnMiss")] public bool InteractOnMiss = false;

    /// <summary>
    ///     If true, and if <see cref="ShowHandItemOverlay"/> is enabled, then this action's icon will be drawn by that
    ///     over lay in place of the currently held item "held item".
    /// </summary>
    [DataField("targetingIndicator")] public bool TargetingIndicator = true;
}

/// <summary>
///     Action that targets some entity. Will result in <see cref="EntityTargetActionEvent"/> being raised.
/// </summary>
[Serializable, NetSerializable]
[Virtual]
public class EntityTargetAction : TargetedAction
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event", true)]
    public EntityTargetActionEvent? Event { get; set; }

    [DataField("serverEvent", serverOnly: true)]
    public EntityTargetActionEvent? ServerEvent { get; }

    [DataField("whitelist")] public EntityWhitelist? Whitelist;

    [DataField("canTargetSelf")] public bool CanTargetSelf = true;
}

/// <summary>
///     Action that targets some map coordinates. Will result in <see cref="WorldTargetActionEvent"/> being raised.
/// </summary>
[Serializable, NetSerializable]
[Virtual]
public class WorldTargetAction : TargetedAction
{
    /// <summary>
    ///     The local-event to raise when this action is performed.
    /// </summary>
    [DataField("event")]
    [NonSerialized]
    public WorldTargetActionEvent? Event;

    // This is a shitty hack to get around the fact that action-prototypes should not in general be sever-exclusive
    // prototypes, but some actions may need to use server-exclusive events, and there is no way to specify on a
    // per-prototype basis whether the client should ignore it when validating yaml.
    [DataField("serverEvent", serverOnly: true)]
    public WorldTargetActionEvent? ServerEvent;
}

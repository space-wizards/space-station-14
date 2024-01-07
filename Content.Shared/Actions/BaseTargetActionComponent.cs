using Content.Shared.Interaction;

namespace Content.Shared.Actions;

public abstract partial class BaseTargetActionComponent : BaseActionComponent
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

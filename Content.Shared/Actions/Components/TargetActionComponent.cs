using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions.Components;

/// <summary>
/// An action that targets an entity or map.
/// Requires <see cref="ActionComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public sealed partial class TargetActionComponent : Component
{
    /// <summary>
    ///     For entity- or map-targeting actions, if this is true the action will remain selected after it is used, so
    ///     it can be continuously re-used. If this is false, the action will be deselected after one use.
    /// </summary>
    [DataField]
    public bool Repeat;

    /// <summary>
    ///     For  entity- or map-targeting action, determines whether the action is deselected if the user doesn't click a valid target.
    /// </summary>
    [DataField]
    public bool DeselectOnMiss;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot actually access the target
    ///     (unobstructed, in inventory, in backpack, etc). Some spells or abilities may want to disable this and
    ///     implement their own checks.
    /// </summary>
    /// <remarks>
    ///     Even if this is false, the <see cref="Range"/> will still be checked.
    /// </remarks>
    [DataField]
    public bool CheckCanAccess = true;

    /// <summary>
    ///     The collision group to use to check for accessibility if <see cref="CheckCanAccess" /> is true.
    /// </summary>
    [DataField]
    public CollisionGroup AccessMask = SharedInteractionSystem.InRangeUnobstructedMask;

    /// <summary>
    ///     The allowed range for a target to be. If zero or negative, the range check is skipped,
    ///     unless <see cref="CheckCanAccess"/> is true.
    /// </summary>
    [DataField]
    public float Range = SharedInteractionSystem.InteractionRange;

    /// <summary>
    ///     If the target is invalid, this bool determines whether the left-click will default to performing a standard-interaction
    /// </summary>
    /// <remarks>
    ///     Interactions will still be blocked if the target-validation generates a pop-up
    /// </remarks>
    [DataField]
    public bool InteractOnMiss;

    /// <summary>
    ///     If true, and if <see cref="ShowHandItemOverlay"/> is enabled, then this action's icon will be drawn by that
    ///     over lay in place of the currently held item "held item".
    /// </summary>
    [DataField]
    public bool TargetingIndicator = true;
}

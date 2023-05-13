using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[NetworkedComponent]
[RegisterComponent]
[Access(typeof(SharedActionsSystem))]
public sealed class ActionsComponent : Component
{
    [ViewVariables]
    [Access(typeof(SharedActionsSystem), Other = AccessPermissions.ReadExecute)]
    // FIXME Friends
    public SortedSet<ActionType> Actions = new();

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class ActionsComponentState : ComponentState
{
    public readonly List<ActionType> Actions;

    public ActionsComponentState(List<ActionType> actions)
    {
        Actions = actions;
    }
}

/// <summary>
///     Determines how the action icon appears in the hotbar for item actions.
/// </summary>
public enum ItemActionIconStyle : byte
{
    /// <summary>
    /// The default - The item icon will be big with a small action icon in the corner
    /// </summary>
    BigItem,

    /// <summary>
    /// The action icon will be big with a small item icon in the corner
    /// </summary>
    BigAction,

    /// <summary>
    /// BigAction but no item icon will be shown in the corner.
    /// </summary>
    NoItem
}

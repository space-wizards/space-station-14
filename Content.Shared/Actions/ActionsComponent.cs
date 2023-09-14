using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Actions;

[NetworkedComponent]
[RegisterComponent]
[Access(typeof(SharedActionsSystem))]
public sealed partial class ActionsComponent : Component
{
    /// <summary>
    ///     Handled on the client to track added and removed actions.
    /// </summary>
    [ViewVariables] public readonly Dictionary<EntityUid, ActionMetaData> OldClientActions = new();

    [ViewVariables] public readonly HashSet<EntityUid> Actions = new();

    public override bool SendOnlyToOwner => true;
}

[Serializable, NetSerializable]
public sealed class ActionsComponentState : ComponentState
{
    public readonly HashSet<NetEntity> Actions;

    public ActionsComponentState(HashSet<NetEntity> actions)
    {
        Actions = actions;
    }
}

public readonly record struct ActionMetaData(bool ClientExclusive, bool AutoRemove);

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

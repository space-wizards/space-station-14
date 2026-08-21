using Content.Shared.Popups;
using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// If the user attempts an action with this component,
/// it is aborted with a custom message while the user is pacified.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DangerousActionComponent : Component
{
    /// <summary>
    /// The popup the action user will see when the user is pacified.
    /// </summary>
    [DataField]
    public LocId PacificationMessage = "dangerous-action-popup";

    /// <summary>
    /// What popup type the pacified message should appear as.
    /// </summary>
    [DataField]
    public PopupType MessageType = PopupType.SmallCaution;
}

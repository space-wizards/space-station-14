using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// If the user attempts an action with this component,
/// it is aborted with a custom message while the user is pacified.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DangerousActionComponent : Component
{
    [DataField]
    public string PacificationMessage { get; set; } = "dangerous-action-popup";
}

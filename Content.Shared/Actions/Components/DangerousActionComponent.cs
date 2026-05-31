using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// If the user attempts an action with this component,
/// they cannot do so if they are pacified.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DangerousActionComponent : Component
{
    [DataField]
    public string PacificationMessage { get; set; } = "dangerous-action-popup";
}

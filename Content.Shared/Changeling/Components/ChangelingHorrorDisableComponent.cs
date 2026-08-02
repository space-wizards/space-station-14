using Robust.Shared.GameStates;

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Marks an action as aware of changeling horror status.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingHorrorDisableComponent : Component
{
    /// <summary>
    /// If true, the action will be toggled off when the horror form is entered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ToggleOff = false;

    /// <summary>
    /// Used to keep track of the disable status before the horror form
    /// </summary>
    [AutoNetworkedField]
    public bool OldToggleStatus = false;
}

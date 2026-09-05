using Robust.Shared.GameStates;

namespace Content.Shared.Actions.Components;

/// <summary>
/// Allows the temporary disabling of an action.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DisableActionComponent : Component
{
    /// <summary>
    /// The duration of disabling the action.
    /// </summary>
    [DataField(required: true)]
    public TimeSpan DisableDuration;

    /// <summary>
    /// When the action is to be re-enabled.
    /// Null means the ability isn't disabled.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? EnableAt;
}

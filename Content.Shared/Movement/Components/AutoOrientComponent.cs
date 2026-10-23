using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Automatically rotates eye upon grid traversals.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class AutoOrientComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextChange;
}

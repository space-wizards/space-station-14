using Robust.Shared.GameStates;

namespace Content.Shared.Alert;

/// <summary>
///     Handles the icons on the right side of the screen.
///     Should only be used for player-controlled entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AlertsComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<AlertKey, AlertState> Alerts = new();

    public override bool SendOnlyToOwner => true;
}

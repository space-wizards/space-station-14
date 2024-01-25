using Robust.Shared.GameStates;

namespace Content.Shared.Alert;

/// <summary>
///     Copy of the entity's alerts that are flagged for autoRemove, so that not all of the alerts need to be checked constantly
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AlertAutoRemoveComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public Dictionary<AlertKey, AlertState> Alerts = new();

    public override bool SendOnlyToOwner => true;
}

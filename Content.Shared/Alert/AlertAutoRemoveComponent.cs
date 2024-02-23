using Robust.Shared.GameStates;

namespace Content.Shared.Alert;

/// <summary>
///     Copy of the entity's alerts that are flagged for autoRemove, so that not all of the alerts need to be checked constantly
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertAutoRemoveComponent : Component
{
    /// <summary>
    ///     List of alerts that have to be checked on every tick for automatic removal at a specific time
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<AlertKey> AlertKeys = new();

    public override bool SendOnlyToOwner => true;
}

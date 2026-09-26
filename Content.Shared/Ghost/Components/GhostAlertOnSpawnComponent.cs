using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Components;

/// <summary>
/// Creates an alert for ghosts on spawn.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostAlertOnSpawnComponent : Component
{
    /// <summary>
    /// Creates an alert for ghosts on entity spawn.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "BaseGhostTeleportAlert";

    /// <summary>
    /// Lifetime of the ghost alert
    /// </summary>
    [DataField]
    public TimeSpan AlertDuration = TimeSpan.FromSeconds(20);
}

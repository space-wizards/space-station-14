using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ghost.Components;

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
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);
}

using Content.Shared.ShipGuns.Components;
using Robust.Shared.GameStates;

namespace Content.Server.ShipGuns.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedTurretConsoleComponent))]
public sealed class TurretConsoleComponent : SharedTurretConsoleComponent
{
    /// <summary>
    /// How much to zoom out when using ship guns.
    /// </summary>
    [DataField("zoom")]
    public Vector2 Zoom = new(1.5f, 1.5f);
}

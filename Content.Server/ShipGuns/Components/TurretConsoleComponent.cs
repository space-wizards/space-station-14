using Content.Shared.ShipGuns.Components;
using Robust.Shared.GameStates;

namespace Content.Server.ShipGuns.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedTurretConsoleComponent))]
public sealed class TurretConsoleComponent : SharedTurretConsoleComponent
{
    // Only one because tracking more than one mouse sounds fucking awful.
    /// <summary>
    /// Who's using the turret console atm
    /// </summary>
    [ViewVariables]
    public GunnerComponent? SubscribedGunner = null;

    /// <summary>
    /// How much to zoom out when using ship guns.
    /// </summary>
    [DataField("zoom")]
    public Vector2 Zoom = new(1.5f, 1.5f);
}

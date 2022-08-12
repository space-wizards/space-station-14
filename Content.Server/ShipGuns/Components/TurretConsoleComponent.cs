using Content.Shared.ShipGuns.Components;
using Robust.Shared.GameStates;

namespace Content.Server.ShipGuns.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedTurretConsoleComponent))]
public sealed class TurretConsoleComponent : SharedTurretConsoleComponent
{
    
}

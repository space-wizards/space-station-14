using Content.Shared.ShipGuns.Components;
using Robust.Shared.GameStates;

namespace Content.Client.ShipGuns.Components;

/// <inheritdoc/>
[RegisterComponent, NetworkedComponent]
[ComponentReference(typeof(SharedTargetingConsoleComponent))]
public sealed class TargetingConsoleComponent : SharedTargetingConsoleComponent
{
    
}

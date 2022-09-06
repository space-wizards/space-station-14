using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

/// <summary>
/// Raises the engine movement inputs for a particular entity onto the designated entity
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class RelayInputMoverComponent : Component
{
    [ViewVariables]
    public EntityUid? RelayEntity;
}

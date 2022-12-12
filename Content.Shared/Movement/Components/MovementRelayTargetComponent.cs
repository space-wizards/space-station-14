using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MovementRelayTargetComponent : Component
{
    /// <summary>
    /// Entities that are relaying to us.
    /// </summary>
    [ViewVariables] public readonly List<EntityUid> Entities = new();
}

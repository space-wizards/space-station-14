using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MovementRelayTargetComponent : Component
{
    // This really shouldn't be a list at the moment. Its just not supported.
    // Neither movement updating, nor HandleDirChange() support more than one mover.
    // Its currently possible for the direction to be set by one mover and the relative rotation to be set by a separate unrelated mover.
    // AAAAA

    /// <summary>
    /// Entities that are relaying to us.
    /// </summary>
    [ViewVariables] public readonly List<EntityUid> Entities = new();
}

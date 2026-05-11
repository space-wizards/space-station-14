using Robust.Shared.GameStates;

namespace Content.Shared._EE.Movement.Components;

/// <summary>
/// Suppresses footstep sounds while the entity has no shoes equipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoShoesSilentFootstepsComponent : Component
{
}

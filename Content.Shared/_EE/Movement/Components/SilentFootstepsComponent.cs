using Robust.Shared.GameStates;

namespace Content.Shared._EE.Movement.Components;

/// <summary>
/// Suppresses footstep sounds always, regardless of whether the entity is wearing shoes.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SilentFootstepsComponent : Component
{
}

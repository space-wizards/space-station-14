using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Tracks whether an entity has ANY stamina damage for update purposes only.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class ActiveStaminaComponent : Component
{

}

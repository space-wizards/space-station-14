using Robust.Shared.GameStates;

namespace Content.Shared.CombatMode.Pacification;

/// <summary>
/// Guns with this component can be fired by pacifists
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PacifismAllowedGunComponent : Component
{
}

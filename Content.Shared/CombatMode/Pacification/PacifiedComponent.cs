using Robust.Shared.GameStates;

namespace Content.Shared.CombatMode.Pacification;

/// <summary>
/// Status effect that disables combat mode.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(PacificationSystem))]
public sealed partial class PacifiedComponent : Component
{

}

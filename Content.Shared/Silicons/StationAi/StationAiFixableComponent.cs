using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Attached to entities that can be repaired others that have an
/// <see cref="StationAiFixerConsoleComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StationAiFixableComponent : Component
{

}

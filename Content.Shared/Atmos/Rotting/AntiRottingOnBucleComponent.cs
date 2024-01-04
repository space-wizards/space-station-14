using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Rotting;

/// <summary>
/// Perishable entities buckled to an entity with this component this will not rot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveAntiRottingOnBuckleComponent : Component
{
}

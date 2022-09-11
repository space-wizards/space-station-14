using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Allows the entity with this component to be placed into the output slot of a ChemMaster.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed class FitsInChemMasterOutputComponent : Component
{
}
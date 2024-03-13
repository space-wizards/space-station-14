using Robust.Shared.GameStates;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Blocks this entity's ability to spill solution containing entities via the verb menu.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PreventSpillerComponent : Component
{

}

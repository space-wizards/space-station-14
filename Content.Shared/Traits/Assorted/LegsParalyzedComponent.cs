using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// Used for Wheelchair bound trait.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{
}

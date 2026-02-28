using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{
    /// <summary>
    /// The speed modifier to apply. Zero by default to completely stop movement.
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.0f;
}

using Content.Shared.Movement.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedSystem))]
public sealed partial class LegsParalyzedComponent : Component
{
    public float BaseWalkSpeed = 0.0f;
    public float BaseSprintSpeed = 0.0f;
    public float Acceleration = 0.0f;
}

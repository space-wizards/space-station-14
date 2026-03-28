using Robust.Shared.GameStates;

namespace Content.Shared.RussStation.Surgery.Components;

/// <summary>
/// Marks furniture as a surgery surface that provides a speed bonus when a patient is buckled to it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgerySurfaceComponent : Component
{
    /// <summary>
    /// Multiplier applied to surgery step durations. Lower = faster.
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.5f;
}

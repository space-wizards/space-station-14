using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Denotes a specific solution contained within this entity that can can be
/// easily "drained". This means things with taps/spigots, or easily poured
/// items.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DrainableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// The drain doafter time required to transfer reagents from the solution.
    /// </summary>
    [DataField]
    public TimeSpan DrainTime = TimeSpan.Zero;
}

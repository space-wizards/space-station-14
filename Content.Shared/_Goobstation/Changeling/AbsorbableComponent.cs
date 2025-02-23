using Robust.Shared.GameStates;

namespace Content.Shared.Changeling;

/// <summary>
///     Component that indicates that a person can be absorbed by a changeling.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbableComponent : Component
{
    [DataField("disabled")]
    public bool Disabled = false;

    /// <summary>
    /// Percentage of biomass restored on consumption.
    /// Smallest animals have the lowest percentages, etc. A lower percentage will also have a faster absorb time.
    /// </summary>
    [DataField]
    public float BiomassRestored = 1f;
}

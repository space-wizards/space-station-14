using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
///     Reagents that can be added easily. For example like
///     pouring something into another beaker, glass, or into the gas
///     tank of a car.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RefillableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can added to easily.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";

    /// <summary>
    /// The maximum amount that can be transferred to the solution at once
    /// </summary>
    [DataField("maxRefill")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2? MaxRefill { get; set; } = null;
}

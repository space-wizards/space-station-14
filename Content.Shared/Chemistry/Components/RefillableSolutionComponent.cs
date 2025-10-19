using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Denotes that the entity has a solution contained which can be easily added
/// to. This should go on things that are meant to be refilled, including
/// pouring things into a beaker. If you run it under a sink tap, it's probably
/// refillable.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RefillableSolutionComponent : Component
{
    /// <summary>
    /// Solution name that can added to easily.
    /// </summary>
    [DataField]
    public string Solution = "default";

    /// <summary>
    /// The maximum amount that can be transferred to the solution at once
    /// </summary>
    [DataField]
    public FixedPoint2? MaxRefill = null;
}

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// Denotes that the entity has a solution contained which can be easily added
/// to in controlled volumes. This should go on things that are meant to be refilled, including
/// pouring things into a beaker. The action for this is represented via clicking.
///
/// To represent it being possible to just dump entire volumes at once into an entity, see <see cref="DumpableSolutionComponent"/>.
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

    /// <summary>
    /// The refill doafter time required to transfer reagents into the solution.
    /// </summary>
    [DataField]
    public TimeSpan RefillTime = TimeSpan.Zero;
}

using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Kitchen.Components;

/// <summary>
/// This component allows an entity to be used in the reagent grinder to extract reagents.
/// <seealso cref="ReagentGrinderComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExtractableComponent : Component
{
    /// <summary>
    /// The reagents to spawn when the grinder is set to juice mode.
    /// This will delete the reagents in the GrindableSolution.
    /// </summary>
    [DataField]
    public Solution? JuiceSolution;

    /// <summary>
    /// The reagents to transfer into the beaker when the grinder is set to grind mode.
    /// </summary>
    [DataField]
    public string? GrindableSolutionName;
};

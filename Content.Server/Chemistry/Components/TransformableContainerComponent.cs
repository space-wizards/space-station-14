using Content.Server.Animals.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;

namespace Content.Server.Chemistry.Components;

/// <summary>
/// A container that transforms its appearance depending on the reagent it contains.
/// It returns to its initial state once the reagent is removed.
/// e.g. An empty glass changes to a beer glass when beer is added to it.
///
/// Should probably be joined with SolutionContainerVisualsComponent when solutions are networked.
/// </summary>
[RegisterComponent, Access(typeof(TransformableContainerSystem))]
public sealed partial class TransformableContainerComponent : Component
{
    /// <summary>
    /// This is the initial metadata name for the container.
    /// It will revert to this when emptied.
    /// It defaults to the name of the parent entity unless overwritten.
    /// </summary>
    [DataField("initialName")]
    public string? InitialName;

    /// <summary>
    /// This is the initial metadata description for the container.
    /// It will revert to this when emptied.
    ///     /// It defaults to the description of the parent entity unless overwritten.
    /// </summary>
    [DataField("initialDescription")]
    public string? InitialDescription;
    /// <summary>
    /// This stores whatever primary reagent is currently in the container.
    /// It is used to help determine if a transformation is needed on solution update.
    /// </summary>
    [DataField("currentReagent")]
    public ReagentPrototype? CurrentReagent;

    /// <summary>
    /// This returns whether this container in a transformed or initial state.
    /// </summary>
    ///
    [DataField("transformed")]
    public bool Transformed;
}

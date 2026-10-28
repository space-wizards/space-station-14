using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Components;

/// <summary>
///     Fills a solution container randomly using a weighted random prototype
/// </summary>
[RegisterComponent, Access(typeof(SolutionRandomFillSystem))]
public sealed partial class RandomFillSolutionComponent : Component
{
    /// <summary>
    ///     Solution name which to add reagents to.
    /// </summary>
    [DataField]
    public string Solution = "solution";

    /// <summary>
    ///     Weighted random fill prototype Id. Used to pick reagent and quantity.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomFillSolutionPrototype>? WeightedRandomId;
}

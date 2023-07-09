using Content.Server.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components.SolutionManager;

/// <summary>
///     Fills a solution container randomly using a weighted random prototype
/// </summary>
[RegisterComponent, Access(typeof(SolutionRandomFillSystem))]
public sealed class RandomFillSolutionComponent : Component
{
    /// <summary>
    ///     Solution name which to add reagents to.
    /// </summary>
    [DataField("solution")]
    public string Solution { get; set; } = "default";

    /// <summary>
    ///     Weighted random prototype Id. Used to pick reagent.
    /// </summary>
    [DataField("weightedRandomId", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string WeightedRandomId { get; set; } = "default";

    /// <summary>
    ///     Amount of reagent to add.
    /// </summary>
    [DataField("quantity")]
    public FixedPoint2 Quantity { get; set; } = 0;
}

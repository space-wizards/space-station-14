using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using System.Text.Json.Serialization;

namespace Content.Shared.Random;

/// <summary>
///     Data that specifies reagents that share the same weight and quantity for use with WeightedRandomSolution.
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class RandomPlantMutation
{
    /// <summary>
    ///     Odds of this mutation occurring with 1 point of mutation severity on a plant.
    /// </summary>
    [DataField("baseOdds")]
    public float BaseOdds = 0;

    /// <summary>
    ///     The name of this mutation.
    /// </summary>
    [DataField("name")]
    public string Name = "";

    /// <summary>
    /// The actual EntityEffect to apply to the target
    /// </summary>
    [JsonPropertyName("mutation")]
    [DataField("mutation")]
    public EntityEffect Mutation = default!;

    [DataField("appliesToProduce")]
    public bool AppliesToProduce = true;
}

using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using System.Text.Json.Serialization;

namespace Content.Shared.Random;

/// <summary>
///     Data that specifies the odds and effects of possible random plant mutations.
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
    //[JsonPropertyName("mutation")]
    [DataField("mutation")]
    public EntityEffect Mutation = default!; //TODO: rename to Effect or something.

    /// <summary>
    /// This mutation will target the harvested produce
    /// </summary>
    [DataField("appliesToProduce")]
    public bool AppliesToProduce = true;

    /// <summary>
    /// This mutation will target the growing plant as soon as this mutation is applied.
    /// </summary>
    [DataField("appliesToPlant")]
    public bool AppliesToPlant = true;
}

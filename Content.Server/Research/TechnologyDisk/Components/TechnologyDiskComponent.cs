using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class TechnologyDiskComponent : Component
{
    /// <summary>
    /// The recipe that will be added. If null, one will be randomly generated
    /// </summary>
    [DataField("recipes")]
    public List<string>? Recipes;

    /// <summary>
    /// A weighted random prototype for how rare each tier should be.
    /// </summary>
    [DataField("tierWeightPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string TierWeightPrototype = "TechDiskTierWeights";
}

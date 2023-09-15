using Content.Server.Botany.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;
using Robust.Shared.Audio;

namespace Content.Server.Botany;

[Prototype("plantTrait")]
public sealed class PlantTraitPrototype : PlantTraitData, IPrototype
{
    [IdDataField] public string ID { get; private init; } = default!;
}

[Virtual, DataDefinition]
[Access(typeof(MutationSystem))]
public partial class PlantTraitData
{
    /// <summary>
    ///     If true, the fruit recieves this traits components.
    /// </summary>
    [DataField("mutationLikelyhood")] public int MutationLikelyhood = 10;

    /// <summary>
    ///     The components that get added to the target plant/fruit, when they have this trait.
    /// </summary>
    [DataField("components")]
    public ComponentRegistry Components { get; private set; } = default!;
}

using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Salvage.Expeditions;

[Prototype("salvageFaction")]
public sealed class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("mobWeights", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<SalvageMobWeight, EntityPrototype>))]
    public Dictionary<string, SalvageMobWeight> MobWeights = default!;

    [ViewVariables]
    public float TotalWeight => MobWeights.Values.Sum(o => o.Weight);

    /// <summary>
    /// Per expedition type data for this faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<IFactionExpeditionConfig, SalvageExpeditionPrototype>))]
    public Dictionary<string, IFactionExpeditionConfig> Configs = new();
}

[DataDefinition]
public record struct SalvageMobWeight
{
    // A mob may be cheap but rare or expensive but frequent.

    /// <summary>
    /// How much it costs to spawn this mob.
    /// </summary>
    [DataField("cost")]
    public float Cost;

    /// <summary>
    /// How frequent is this mob.
    /// </summary>
    [DataField("weight")]
    public float Weight;
}

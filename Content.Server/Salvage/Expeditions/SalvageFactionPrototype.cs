using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Salvage.Expeditions;

[Prototype("salvageFaction")]
public sealed class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("mobWeights", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<float, EntityPrototype>))]
    public Dictionary<string, float> MobWeights = default!;

    /// <summary>
    /// Per expedition type data for this faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<IFactionExpeditionConfig, SalvageExpeditionPrototype>))]
    public Dictionary<string, IFactionExpeditionConfig> Configs = new();
}

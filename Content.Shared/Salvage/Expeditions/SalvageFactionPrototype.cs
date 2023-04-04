using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Salvage.Expeditions;

[Prototype("salvageFaction")]
public sealed class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("groups", required: true)]
    public List<SalvageMobGroup> MobGroups = default!;

    /// <summary>
    /// Per expedition type data for this faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<IFactionExpeditionConfig, SalvageExpeditionPrototype>))]
    public Dictionary<string, IFactionExpeditionConfig> Configs = new();
}

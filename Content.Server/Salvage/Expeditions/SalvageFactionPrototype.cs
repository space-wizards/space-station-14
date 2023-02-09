using Content.Shared.Salvage;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Salvage.Expeditions;

[Prototype("salvageFaction")]
public sealed class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("groups", required: true)]
    public List<SalvageMobGroup> MobGroups = default!;

    /// <summary>
    /// Per expedition type data for this faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<IFactionExpeditionConfig, SalvageExpeditionPrototype>))]
    public Dictionary<string, IFactionExpeditionConfig> Configs = new();
}

[DataDefinition]
public record struct SalvageMobGroup()
{
    // A mob may be cheap but rare or expensive but frequent.

    /// <summary>
    /// Probability to spawn this group. Summed with everything else for the faction.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("prob")]
    public float Prob = 1f;

    [ViewVariables(VVAccess.ReadWrite), DataField("entries", required: true)]
    public List<EntitySpawnEntry> Entries = new();
}

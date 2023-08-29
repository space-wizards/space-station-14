using Content.Shared.Salvage.Expeditions.Modifiers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Salvage.Expeditions;

[Prototype("salvageFaction")]
public sealed class SalvageFactionPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite), DataField("entries", required: true)]
    public List<SalvageMobEntry> MobGroups = new();

    /// <summary>
    /// Miscellaneous data for factions.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("configs")]
    public Dictionary<string, string> Configs = new();
}

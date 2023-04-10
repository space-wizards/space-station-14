using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Spawned inside of a salvage mission.
/// </summary>
[Prototype("salvageLoot")]
public sealed class SalvageLootPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField("desc")] public string Description = string.Empty;

    /// <summary>
    /// All of the loot rules
    /// </summary>
    [DataField("loots")]
    public List<IDungeonLoot> LootRules = new();
}

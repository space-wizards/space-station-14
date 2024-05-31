using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Spawned inside of a salvage mission.
/// </summary>
[Prototype("salvageLoot")]
public sealed partial class SalvageLootPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Should this loot always spawn if possible. Used for stuff such as ore.
    /// </summary>
    [DataField("guaranteed")] public bool Guaranteed;

    /// <summary>
    /// All of the loot rules
    /// </summary>
    [DataField("loots")]
    public List<IDungeonLoot> LootRules = new();
}

using Content.Shared.Salvage;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Procedural.Loot;

/// <summary>
/// Spawned inside of a salvage mission.
/// </summary>
[Prototype("salvageLoot")]
public sealed class SalvageLootPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Should this loot always spawn if possible. Used for stuff such as ore.
    /// </summary>
    [DataField("guaranteed")] public bool Guaranteed;

    [DataField("desc")] public string Description = string.Empty;

    /// <summary>
    /// Mission types this loot is not allowed to spawn for
    /// </summary>
    [DataField("blacklist")]
    public List<SalvageMissionType> Blacklist = new();

    /// <summary>
    /// All of the loot rules
    /// </summary>
    [DataField("loots")]
    public List<IDungeonLoot> LootRules = new();
}

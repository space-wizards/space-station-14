using Robust.Shared.Prototypes;

namespace Content.Shared.Procedural.Loot;

[Prototype("loot")]
public sealed class LootPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// All of the loot rules
    /// </summary>
    [DataField("loots")]
    public List<IDungeonLoot> Loots = new();
}

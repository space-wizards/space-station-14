using Content.Shared.Random;

namespace Content.Server.Mining.Components;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class MineableComponent : Component
{
    [DataField("oreChance"), ViewVariables(VVAccess.ReadWrite)]
    public float OreChance = 0.5f;

    [DataField("oreRarityPrototypeId", customTypeSerializer: typeof(WeightedRandomPrototype))]
    public string? OreRarityPrototypeId;

    [DataField("currentOre"), ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentOre;
}

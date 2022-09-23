using Content.Shared.Random;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Mining.Components;

/// <summary>
/// Defines an entity that will drop a random ore after being destroyed.
/// </summary>
[RegisterComponent]
public sealed class MineableComponent : Component
{
    /// <summary>
    /// How often an entity will be seeded with ore. Note: the amount of ore
    /// that is dropped is dependent on the ore prototype. <see crefalso="OrePrototype"/>
    /// </summary>
    [DataField("oreChance"), ViewVariables(VVAccess.ReadWrite)]
    public float OreChance = 0.1f;

    /// <summary>
    /// The weighted random prototype used for determining what ore will be dropped.
    /// </summary>
    [DataField("oreRarityPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string? OreRarityPrototypeId;

    /// <summary>
    /// The ore that this entity holds.
    /// If set in the prototype, it will not be overriden.
    /// </summary>
    [DataField("currentOre"), ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentOre;
}

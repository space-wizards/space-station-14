using Content.Shared.Mining;
using Content.Shared.Random;
using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Mining.Components;

/// <summary>
/// Defines an entity that will drop a random ore after being destroyed.
/// </summary>
[RegisterComponent]
public sealed class OreVeinComponent : Component
{
    /// <summary>
    /// How often an entity will be seeded with ore. Note: the amount of ore
    /// that is dropped is dependent on the ore prototype. <see crefalso="OrePrototype"/>
    /// </summary>
    [DataField("oreChance")]
    public float OreChance = 0.1f;

    /// <summary>
    /// The weighted random prototype used for determining what ore will be dropped.
    /// </summary>
    [DataField("oreRarityPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<WeightedRandomPrototype>))]
    public string? OreRarityPrototypeId;

    /// <summary>
    /// The weighted random prototype used for determining what ore will be dropped.
    /// Keyed to a specific tool.
    /// </summary>
    [DataField("mappedTools", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<string, WeightedRandomPrototype>))]
    public Dictionary<string, string>? MappedTools = new();

    /// <summary>
    /// The ore that this entity holds.
    /// If set in the prototype, it will not be overriden.
    /// </summary>
    [DataField("currentOre", customTypeSerializer: typeof(PrototypeIdSerializer<OrePrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentOre;

    /// <summary>
    ///     The radius of the circle that the dropped entities can be randomly spawned in.
    ///     Centered on the entity.
    /// </summary>
    [DataField("radius")]
    public float Radius = 0.2f;
}

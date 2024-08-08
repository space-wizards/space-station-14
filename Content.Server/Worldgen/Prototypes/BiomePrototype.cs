using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Server.Worldgen.Prototypes;

/// <summary>
///     This is a prototype for biome selection, allowing the component list of a chunk to be amended based on the output
///     of noise channels at that location.
/// </summary>
[Prototype("spaceBiome")]
public sealed partial class BiomePrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc />
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<EntityPrototype>))]
    public string[]? Parents { get; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <inheritdoc />
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The valid ranges of noise values under which this biome can be picked.
    /// </summary>
    [DataField("noiseRanges", required: true)]
    public Dictionary<string, List<Vector2>> NoiseRanges = default!;

    /// <summary>
    ///     Higher priority biomes get picked before lower priority ones.
    /// </summary>
    [DataField("priority", required: true)]
    public int Priority { get; private set; }

    /// <summary>
    ///     The components that get added to the target map.
    /// </summary>
    [DataField("chunkComponents")]
    [AlwaysPushInheritance]
    public ComponentRegistry ChunkComponents { get; } = new();

    //TODO: Get someone to make this a method on componentregistry that does it Correctly.
    /// <summary>
    ///     Applies the worldgen config to the given target (presumably a map.)
    /// </summary>
    public void Apply(EntityUid target, ISerializationManager serialization, IEntityManager entityManager)
    {
        // Add all components required by the prototype. Engine update for this whenst.
        foreach (var data in ChunkComponents.Values)
        {
            var comp = (Component) serialization.CreateCopy(data.Component, notNullableOverride: true);
            entityManager.AddComponent(target, comp);
        }
    }
}


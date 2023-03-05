using Content.Server._Citadel.Worldgen.Prototypes;
using Content.Server._Citadel.Worldgen.Tools;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Citadel.Worldgen.Components.Debris;

/// <summary>
///     This is used for selecting debris with a probability determined by a noise channel.
///     Takes priority over SimpleDebrisSelectorComponent and should likely be used in combination.
/// </summary>
[RegisterComponent]
public sealed class NoiseDrivenDebrisSelectorComponent : Component
{
    private EntitySpawnCollectionCache? _cache;

    [DataField("debrisTable", required: true)]
    private List<EntitySpawnEntry> _entries = default!;

    /// <summary>
    ///     The debris entity spawn collection.
    /// </summary>
    public EntitySpawnCollectionCache CachedDebrisTable
    {
        get
        {
            _cache ??= new EntitySpawnCollectionCache(_entries);
            return _cache;
        }
    }

    /// <summary>
    ///     The noise channel to use as a density controller.
    /// </summary>
    /// <remarks>This noise channel should be mapped to exactly the range [0, 1] unless you want a lot of warnings in the log.</remarks>
    [DataField("noiseChannel", customTypeSerializer: typeof(PrototypeIdSerializer<NoiseChannelPrototype>))]
    public string NoiseChannel { get; } = default!;
}


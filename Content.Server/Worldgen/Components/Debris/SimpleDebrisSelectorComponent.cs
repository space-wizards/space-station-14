using Content.Server._Citadel.Worldgen.Systems.Debris;
using Content.Server._Citadel.Worldgen.Tools;
using Content.Shared.Storage;

namespace Content.Server._Citadel.Worldgen.Components.Debris;

/// <summary>
///     This is used for a very simple debris selection for simple biomes. Just uses a spawn table.
/// </summary>
[RegisterComponent]
[Access(typeof(DebrisFeaturePlacerSystem))]
public sealed class SimpleDebrisSelectorComponent : Component
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
}


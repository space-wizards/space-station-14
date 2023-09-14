using Content.Server.Worldgen.Systems.Debris;
using Content.Server.Worldgen.Tools;
using Content.Shared.Storage;

namespace Content.Server.Worldgen.Components.Debris;

/// <summary>
///     This is used for a very simple debris selection for simple biomes. Just uses a spawn table.
/// </summary>
[RegisterComponent]
[Access(typeof(DebrisFeaturePlacerSystem))]
public sealed partial class SimpleDebrisSelectorComponent : Component
{
    private EntitySpawnCollectionCache? _cache;

    /// <summary>
    ///     The prototype-facing debris table entries.
    /// </summary>
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


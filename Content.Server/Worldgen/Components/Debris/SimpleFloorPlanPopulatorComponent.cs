using System.Linq;
using Content.Server.Worldgen.Systems.Debris;
using Content.Server.Worldgen.Tools;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Worldgen.Components.Debris;

/// <summary>
///     This is used for populating a grid with random entities automatically.
/// </summary>
[RegisterComponent]
[Access(typeof(SimpleFloorPlanPopulatorSystem))]
public sealed partial class SimpleFloorPlanPopulatorComponent : Component
{
    private Dictionary<string, EntitySpawnCollectionCache>? _caches;

    /// <summary>
    ///     The prototype facing floor plan populator entries.
    /// </summary>
    [DataField("entries", required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<List<EntitySpawnEntry>, ContentTileDefinition>))]
    private Dictionary<string, List<EntitySpawnEntry>> _entries = default!;

    /// <summary>
    ///     The spawn collections used to place entities on different tile types.
    /// </summary>
    [ViewVariables]
    public Dictionary<string, EntitySpawnCollectionCache> Caches
    {
        get
        {
            if (_caches is null)
            {
                _caches = _entries
                    .Select(x =>
                        new KeyValuePair<string, EntitySpawnCollectionCache>(x.Key,
                            new EntitySpawnCollectionCache(x.Value)))
                    .ToDictionary(x => x.Key, x => x.Value);
            }

            return _caches;
        }
    }
}


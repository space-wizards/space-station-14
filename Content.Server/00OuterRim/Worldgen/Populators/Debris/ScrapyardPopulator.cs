using Content.Server._00OuterRim.Worldgen.Systems;
using Content.Shared.Maps;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server._00OuterRim.Worldgen.Populators.Debris;

public sealed class ScrapyardPopulator : DebrisPopulator
{
    [DataField("entityTable", required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<List<EntitySpawnEntry>, ContentTileDefinition>))]
    public Dictionary<string, List<EntitySpawnEntry>> EntityTable = default!;

    public override void Populate(EntityUid gridEnt, IMapGrid grid)
    {
        var deferred = EntitySystem.Get<DeferredSpawnSystem>();

        foreach (var tile in grid.GetAllTiles())
        {
            var name = tile.Tile.GetContentTileDefinition().Name;
            if (!EntityTable.ContainsKey(name)) continue;

            var coords = grid.GridTileToLocal(tile.GridIndices);

            foreach (var spawn in EntitySpawnCollection.GetSpawns(EntityTable[name]))
            {
                if (spawn is not null)
                    deferred.SpawnEntityDeferred(spawn, coords);
            }
        }
    }
}

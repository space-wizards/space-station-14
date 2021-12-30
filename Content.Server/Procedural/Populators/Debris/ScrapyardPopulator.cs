using System.Collections.Generic;
using Content.Server.Storage;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Server.Procedural.Populators.Debris;

public class ScrapyardPopulator : DebrisPopulator
{
    [DataField("entityTable", required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<List<EntitySpawnEntry>, ContentTileDefinition>))]
    public Dictionary<string, List<EntitySpawnEntry>> EntityTable = default!;

    public override void Populate(EntityUid gridEnt, IMapGrid grid)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();

        foreach (var tile in grid.GetAllTiles())
        {
            var name = tile.Tile.GetContentTileDefinition().DisplayName;
            if (!EntityTable.ContainsKey(name)) continue;

            var coords = grid.GridTileToLocal(tile.GridIndices);
            var alreadySpawnedGroups = new List<string>();
            foreach (var entry in EntityTable[name])
            {
                if (!string.IsNullOrEmpty(entry.GroupId) &&
                    alreadySpawnedGroups.Contains(entry.GroupId)) continue;

                if (!random.Prob(entry.SpawnProbability))
                    continue;

                for (var i = 0; i < entry.Amount; i++)
                {
                    entityManager.SpawnEntity(entry.PrototypeId, coords);
                }

                if (!string.IsNullOrEmpty(entry.GroupId)) alreadySpawnedGroups.Add(entry.GroupId);
            }
        }
    }
}

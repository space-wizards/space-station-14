using Content.Server.Procedural.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Procedural.Populators.Debris;

public class FillDebrisPopulator : DebrisPopulator
{
    /// <summary>
    /// The "fill" entity to use to fill up most of the asteroid's space.
    /// </summary>
    [DataField("fillerEntity", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string FillerEntity { get; } = default!;

    public override void Populate(EntityUid gridEnt, IMapGrid grid)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var deferred = EntitySystem.Get<DeferredSpawnSystem>();
        var i = 0;
        foreach (var tile in grid.GetAllTiles())
        {
            i++;
            var coords = grid.GridTileToLocal(tile.GridIndices);
            deferred.SpawnEntityDeferred(FillerEntity, coords);
        }
        Logger.InfoS("worldgen", $"Filled {i} tiles with {FillerEntity}.");
    }
}

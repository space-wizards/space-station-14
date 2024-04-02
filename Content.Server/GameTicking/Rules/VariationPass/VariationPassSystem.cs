using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
///     Base class for procedural variation rule passes, which apply some kind of variation to a station,
///     so we simply reduce the boilerplate for the event handling a bit with this.
/// </summary>
public abstract class VariationPassSystem<T> : GameRuleSystem<T>
    where T: IComponent
{
    [Dependency] protected readonly StationSystem Stations = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly MapSystem Map = default!;
    protected  EntityQuery<MapGridComponent> _mapgridQuery;
    public override void Initialize()
    {
        base.Initialize();
        _mapgridQuery = GetEntityQuery<MapGridComponent>();

        SubscribeLocalEvent<T, StationVariationPassEvent>(ApplyVariation);
    }

    protected bool IsMemberOfLargestStationGrid(Entity<TransformComponent> ent, ref StationVariationPassEvent args)
    {
        if (TryComp<MapGridComponent>(ent, out _))
        {
            if (CompOrNull<StationMemberComponent>(ent)?.Station is not { } associatedStationUid)
            {
                return false;
            }
            return associatedStationUid == args.Station.Owner;
        }
        return false;
    }

    protected abstract void ApplyVariation(Entity<T> ent, ref StationVariationPassEvent args);

    /// <summary>
    ///     Returns a list containing all tiles from a specific station 
    /// </summary>
    protected IEnumerable<Robust.Shared.Map.TileRef>? GetAllTilesFromLargestGrid(EntityUid ent, StationDataComponent component, out MapGridComponent? largestGridComponent)
    {
        var largestStationGridUid = Stations.GetLargestGrid(component);
        _mapgridQuery.TryGetComponent(largestStationGridUid, out var largestStationGridComponent);
        largestGridComponent = largestStationGridComponent;

        if (largestStationGridComponent is not null)
        {
            return Map.GetAllTiles(ent, largestStationGridComponent);
        }
        return null;
    }

    /// <summary>
    ///     Taking an IEnumerable<Robust.Shared.Map.TileRef> list, randomly place [numberOfTiles] 
    ///     tiles in a new IEnumerable<Robust.Shared.Map.TileRef> list
    /// </summary> 
    protected IEnumerable<Robust.Shared.Map.TileRef> GetRandomTiles(IEnumerable<Robust.Shared.Map.TileRef> tileRefs, int numberOfTiles)
    {
        var totalTiles = tileRefs.Count();
        for (int i = 0; i < numberOfTiles; i++)
        {
            int randomTileIndex = Random.Next(totalTiles);
            yield return tileRefs.ElementAt(randomTileIndex);
        }
    }
}

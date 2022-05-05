using Robust.Shared.Map;

namespace Content.Server.Maps;

public sealed partial class GameMapSystem
{
    private List<string> _previousMaps = new();

    /// <summary>
    /// All previously loaded maps.
    /// </summary>
    public IReadOnlyList<string> PreviousMaps => _previousMaps;

    private HashSet<EntityUid> _currentlyLoadedMaps = new();

    /// <summary>
    /// All maps loaded in the current round. This provides their respective bookkeeper, which lets you look up information about the map.
    /// </summary>
    public IReadOnlySet<EntityUid> CurrentlyLoadedMaps => _currentlyLoadedMaps;

    private void InitializeBookkeeping()
    {
        SubscribeLocalEvent<PartOfMapComponent, ComponentShutdown>(OnGridDeletion);
        SubscribeLocalEvent<PartOfMapComponent, PostGridSplitEvent>(OnGridSplit);
        SubscribeLocalEvent<MapBookkeepingComponent, ComponentShutdown>(OnBookkeeperDeletion);
    }

    private void OnGridSplit(EntityUid uid, PartOfMapComponent component, PostGridSplitEvent args)
    {
        var bookkeeping = Comp<MapBookkeepingComponent>(component.MapBookkeeper);
        bookkeeping.ComponentGrids.Add(args.Grid);
    }

    private void OnGridDeletion(EntityUid uid, PartOfMapComponent component, ComponentShutdown args)
    {
        var bookkeeping = Comp<MapBookkeepingComponent>(component.MapBookkeeper);
        bookkeeping.ComponentGrids.Remove(uid);

        if (bookkeeping.ComponentGrids.Count == 0)
            Del(component.MapBookkeeper);
    }

    private void OnBookkeeperDeletion(EntityUid uid, MapBookkeepingComponent component, ComponentShutdown args)
    {
        _currentlyLoadedMaps.Remove(uid);
    }


    /// <summary>
    /// Creates the bookkeeper for a map.
    /// </summary>
    /// <param name="proto">Prototype to document.</param>
    /// <param name="grids">Grids to assign.</param>
    /// <returns>The bookkeeper entity for the map.</returns>
    private EntityUid StartBookkeepingMap(GameMapPrototype proto, IReadOnlyList<GridId> grids)
    {
        // mfw copying the null-space hack.
        var bookkeeper = Spawn(null, new MapCoordinates(0, 0, _gameTicker.DefaultMap));
        var bookkeeping = AddComp<MapBookkeepingComponent>(bookkeeper);
        bookkeeping.MapName = proto.MapName;
        bookkeeping.Prototype = proto.ID;

        foreach (var grid in grids)
        {
            var eid = _mapManager.GetGridEuid(grid);
            bookkeeping.ComponentGrids.Add(eid);
            var mapPart = AddComp<PartOfMapComponent>(eid);
            mapPart.MapBookkeeper = bookkeeper;
        }

        return bookkeeper;
    }
}

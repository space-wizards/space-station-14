using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.EntitySystems;
using Robust.Server.GameObjects;

namespace Content.Server.Construction;

public sealed class AnchorableSystem : SharedAnchorableSystem
{
    [Dependency] private readonly StationSystem _stationSystem = null!;
    [Dependency] private readonly TransformSystem _transform = null!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var allGrids = args.NewGrids.ToList();

        var query = AllEntityQuery<AnchorOnlyOnStationComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out _, out var entXform))
        {
            if (entXform.GridUid == null)
                continue;

            if (!allGrids.Contains(entXform.GridUid.Value))
                continue;

            var entityParent = Comp<TransformComponent>(ent).ParentUid;
            var isOnStation = _stationSystem.GetStations()
                .Select(stationEnt => _stationSystem.GetLargestGrid(stationEnt))
                .Contains(entityParent);

            if (isOnStation)
                continue;

            _transform.Unanchor(ent);
        }
    }
}

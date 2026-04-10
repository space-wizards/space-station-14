using System.Linq;
using Content.Server.Station.Systems;
using Content.Shared.Construction.Components;
using Robust.Server.GameObjects;

namespace Content.Server.Construction;

public sealed class AnchorOnlyOnStationSystem : EntitySystem
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

            if (_stationSystem.IsOnStation(ent))
                continue;

            _transform.Unanchor(ent);
        }
    }
}

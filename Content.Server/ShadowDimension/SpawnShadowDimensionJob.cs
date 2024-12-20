using System.Threading;
using System.Threading.Tasks;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.ShadowDimension;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ShadowDimension;

public sealed class SpawnShadowDimensionJob : Job<bool>
{
    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IMapManager _mapManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly AnchorableSystem _anchorable;
    private readonly MetaDataSystem _metaData;
    private readonly SharedTransformSystem _xforms;
    private readonly StationSystem _stationSystem;
    private readonly SharedMapSystem _map;

    public readonly EntityUid Station;
    private readonly ShadowDimensionParams _missionParams;

    private readonly ISawmill _sawmill;

    public SpawnShadowDimensionJob(
        double maxTime,
        IEntityManager entManager,
        IGameTiming timing,
        ILogManager logManager,
        IMapManager mapManager,
        IPrototypeManager protoManager,
        AnchorableSystem anchorable,
        MetaDataSystem metaData,
        SharedTransformSystem xform,
        StationSystem stationSystem,
        SharedMapSystem map,
        EntityUid station,
        ShadowDimensionParams missionParams,
        CancellationToken cancellation = default) : base(maxTime, cancellation)
    {
        _entManager = entManager;
        _timing = timing;
        _mapManager = mapManager;
        _prototypeManager = protoManager;
        _anchorable = anchorable;
        _metaData = metaData;
        _xforms = xform;
        _stationSystem = stationSystem;
        _map = map;
        Station = station;
        _missionParams = missionParams;
        _sawmill = logManager.GetSawmill("shadow_dimension_job");
    }

    protected override async Task<bool> Process()
    {
        if (!_entManager.TryGetComponent<StationDataComponent>(Station, out var stationData))
            return false;

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        //Create new map and set name
        var mapUid = _map.CreateMap(out var mapId, runMapInit: false);
        var stationMetaData = _entManager.EnsureComponent<MetaDataComponent>(Station);
        _metaData.SetEntityName(
            mapUid,
            $"Shadow side of {stationMetaData.EntityName}"); //TODO: Localize it

        _sawmill.Debug("shadow_dimension", $"Spawning station {stationMetaData.EntityName} shadow side with seed {_missionParams.Seed}");
        var random = new Random(_missionParams.Seed);

        return true;
    }
}

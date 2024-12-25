using System.Threading;
using Content.Server.Station.Components;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Maps;
using Content.Shared.AlternateDimension;
using Content.Shared.Tag;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.AlternateDimension;

public sealed partial class AlternateDimensionSystem : SharedAlternateDimensionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tileManager = default!;
    [Dependency] private readonly TileSystem _tileSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private readonly JobQueue _jobQueue = new();
    private readonly List<(SpawnAlternateDimensionJob Job, CancellationTokenSource CancelToken)> _jobs = new();
    private const double JobTime = 0.002;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationAlternateDimensionGeneratorComponent, StationPostInitEvent>(OnStationInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        _jobQueue.Process();

        foreach (var (job, cancelToken) in _jobs.ToArray())
        {
            switch (job.Status)
            {
                case JobStatus.Finished:
                    _jobs.Remove((job, cancelToken));
                    break;
            }
        }
    }

    private void OnStationInit(Entity<StationAlternateDimensionGeneratorComponent> ent, ref StationPostInitEvent args)
    {
        if (!TryComp<StationDataComponent>(ent, out var stationData))
            return;

        var alterParams = new AlternateDimensionParams
        {
            Seed = _random.Next(),
            Dimension = ent.Comp.Dimension
        };

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        if (stationGrid is null)
            return;

        MakeAlternativeRealityGrid(stationGrid.Value, alterParams);
    }

    public void MakeAlternativeRealityGrid(EntityUid originalGrid, AlternateDimensionParams args)
    {
        //Block alternative dimensions generation of the same type
        var realGridComp = EnsureComp<RealDimensionGridComponent>(originalGrid);
        if (realGridComp.Alternatives.ContainsKey(args.Dimension))
            return;

        //Create and setup map
        var alternateMap = _mapSystem.CreateMap(out var alternateMapId, false);
        var gridMetaData = EntityManager.EnsureComponent<MetaDataComponent>(originalGrid);

        //Create and setup grid
        var alternateGrid = _mapManager.CreateGridEntity(alternateMapId);
        var dimenComp = EnsureComp<AlternateDimensionGridComponent>(alternateGrid);
        dimenComp.DimensionType = args.Dimension;
        dimenComp.RealDimensionGrid = originalGrid;
        _metaData.SetEntityName(
            alternateGrid,
            $"{gridMetaData.EntityName} ({args.Dimension})");

        realGridComp.Alternatives.Add(args.Dimension, alternateGrid);

        var cancelToken = new CancellationTokenSource();
        var job = new SpawnAlternateDimensionJob(
            JobTime,
            EntityManager,
            _mapManager,
            _prototypeManager,
            _mapSystem,
            _tileManager,
            _tileSystem,
            _lookup,
            _tag,
            alternateMapId,
            alternateGrid,
            originalGrid,
            args,
            cancelToken.Token);

        _jobs.Add((job, cancelToken));
        _jobQueue.EnqueueJob(job);
    }
}

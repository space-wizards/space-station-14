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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.AlternateDimension;

public sealed class AlternateDimensionSystem : SharedAlternateDimensionSystem
{
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
            Dimension = ent.Comp.Dimension,
        };

        var stationGrid = _stationSystem.GetLargestGrid(stationData);

        if (stationGrid is null)
            return;

        MakeAlternativeRealityGrid(stationGrid.Value, alterParams);
    }

    /// <summary>
    /// Creates a new map and a new grid on this map, based on the parameters of the alternate dimension and the original grid.
    /// </summary>
    /// <param name="originalGrid">Grid, the form and contents of which will be used to create a new grid.</param>
    public bool MakeAlternativeRealityGrid(EntityUid originalGrid, AlternateDimensionParams args)
    {
        //Block alternative dimensions generation of the same type
        var realGridComp = EnsureComp<RealDimensionGridComponent>(originalGrid);
        if (realGridComp.AlternativeGrids.ContainsKey(args.Dimension))
            return false;

        //Create and setup map
        _mapSystem.CreateMap(out var alternateMapId, false);
        var gridMetaData = EntityManager.EnsureComponent<MetaDataComponent>(originalGrid);

        //Create and setup grid
        var alternateGrid = _mapManager.CreateGridEntity(alternateMapId);
        var dimensionComp = EnsureComp<AlternateDimensionGridComponent>(alternateGrid);
        dimensionComp.DimensionType = args.Dimension;
        dimensionComp.RealDimensionGrid = originalGrid;
        _metaData.SetEntityName(
            alternateGrid,
            $"{gridMetaData.EntityName} ({args.Dimension})");

        realGridComp.AlternativeGrids.Add(args.Dimension, alternateGrid);

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

        return
            true; //TODO: Job can fail for various reasons, in which case you need to handle and delete setuped components separately.
    }

    /// <summary>
    /// Trying to find an alternate version of the grid. If found, deletes the map on which this grid is located.
    /// </summary>
    /// <param name="originalGrid">A real grid located on the main game maps. </param>
    /// <param name="type">The type of alternate version of the grid to be deleted. (A large number of different types of alternate grid versions are supported)</param>
    public bool RemoveAlternateRealityGrid(EntityUid originalGrid, ProtoId<AlternateDimensionPrototype> type)
    {
        if (!TryComp<RealDimensionGridComponent>(originalGrid, out var realDimension))
            return false;

        if (!realDimension.AlternativeGrids.TryGetValue(type, out var alternativeGrid))
            return false;

        realDimension.AlternativeGrids.Remove(type);
        QueueDel(Transform(alternativeGrid).MapUid);
        return true;
    }

    /// <summary>
    /// Finds and returns an alternate version of a grid of the specified type.
    /// </summary>
    public EntityUid? GetAlternateRealityGrid(EntityUid originalGrid, ProtoId<AlternateDimensionPrototype> type)
    {
        if (!TryComp<RealDimensionGridComponent>(originalGrid, out var realDimension))
            return null;

        if (!realDimension.AlternativeGrids.TryGetValue(type, out var alternativeGrid))
            return null;

        return alternativeGrid;
    }

    /// <summary>
    /// Tries to find an alternate dimension of the grid the entity is on, and get the same coordinates
    /// in the alternate dimension that the entity is in in the real world at the current moment.
    /// </summary>
    public EntityCoordinates? GetAlternateRealityCoordinates(EntityUid entity,
        ProtoId<AlternateDimensionPrototype> type)
    {
        var xform = Transform(entity);
        if (!TryComp<RealDimensionGridComponent>(xform.GridUid, out var realDimension))
            return null;

        if (!realDimension.AlternativeGrids.TryGetValue(type, out var alternativeGrid))
            return null;

        var alternativeMap = Transform(alternativeGrid).MapUid;
        if (alternativeMap is null)
            return null;

        return new EntityCoordinates(alternativeMap.Value, xform.Coordinates.Position);
    }
}

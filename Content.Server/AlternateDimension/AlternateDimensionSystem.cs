using System.Threading;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.Maps;
using Content.Shared.AlternateDimension;
using Content.Shared.Tag;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
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

        SubscribeLocalEvent<StationAlternateDimensionComponent, StationPostInitEvent>(OnStationInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;
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

    private void OnStationInit(Entity<StationAlternateDimensionComponent> ent, ref StationPostInitEvent args)
    {
        var testParams = new AlternateDimensionParams
        {
            Seed = _random.Next(),
            Dimension = ent.Comp.Dimension
        };

        SpawnStationAlternateDimension(ent, testParams);
    }

    private void SpawnStationAlternateDimension(EntityUid station, AlternateDimensionParams args)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new SpawnAlternateDimensionJob(
            JobTime,
            EntityManager,
            _logManager,
            _mapManager,
            _prototypeManager,
            _metaData,
            _stationSystem,
            _mapSystem,
            _tileManager,
            _tileSystem,
            _lookup,
            _tag,
            station,
            args,
            cancelToken.Token);

        _jobs.Add((job, cancelToken));
        _jobQueue.EnqueueJob(job);
    }
}

using System.Threading;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.ShadowDimension;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ShadowDimension;

public sealed partial class ShadowDimensionSystem : SharedShadowDimensionSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AnchorableSystem _metaData = default!;
    [Dependency] private readonly MetaDataSystem _anchorable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    private readonly JobQueue _jobQueue = new();
    private readonly List<(SpawnShadowDimensionJob Job, CancellationTokenSource CancelToken)> _jobs = new();
    private const double ShadowJobTime = 0.002;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationShadowDimensionComponent, StationPostInitEvent>(OnStationInit);
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

    private void OnStationInit(Entity<StationShadowDimensionComponent> ent, ref StationPostInitEvent args)
    {
        var testParams = new ShadowDimensionParams {Seed = _random.Next()};

        SpawnStationShadowDimension(ent, testParams);
    }

    private void SpawnStationShadowDimension(EntityUid station, ShadowDimensionParams shadowParams)
    {
        var cancelToken = new CancellationTokenSource();
        var job = new SpawnShadowDimensionJob(
            ShadowJobTime,
            EntityManager,
            _timing,
            _logManager,
            _mapManager,
            _prototypeManager,
            _metaData,
            _anchorable,
            _transform,
            _stationSystem,
            _mapSystem,
            station,
            shadowParams,
            cancelToken.Token);


        _jobs.Add((job, cancelToken));
        _jobQueue.EnqueueJob(job);
    }
}

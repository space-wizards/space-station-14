using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Backmen.Blob.Components;
using Content.Shared.Backmen.Blob.Components;
using Content.Shared.Destructible;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.CPUJob.JobQueues;
using Robust.Shared.CPUJob.JobQueues.Queues;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Backmen.Blob;

public sealed class BlobNodeSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly BlobCoreSystem _blobCoreSystem = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;

    private EntityQuery<BlobTileComponent> _tileQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlobNodeComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<BlobNodeComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<BlobNodeComponent, BlobNodePulseEvent>(OnNodePulse);

        _tileQuery = GetEntityQuery<BlobTileComponent>();
    }

    private void OnNodePulse(Entity<BlobNodeComponent> ent, ref BlobNodePulseEvent args)
    {
        var xform = Transform(ent);

        var evSpecial = new BlobSpecialGetPulseEvent();
        foreach (var special in GetSpecialBlobsTiles(ent))
        {
            RaiseLocalEvent(special, evSpecial);
        }

        foreach (var lookupUid in _lookup.GetEntitiesInRange<BlobMobComponent>(xform.Coordinates, ent.Comp.PulseRadius))
        {
            if (_mob.IsDead(lookupUid))
                continue;
            var evMob = new BlobMobGetPulseEvent
            {
                BlobEntity = GetNetEntity(lookupUid),
            };
            RaiseLocalEvent(lookupUid, evMob);
            RaiseNetworkEvent(evMob, Filter.Pvs(lookupUid));
        }
    }

    private const double PulseJobTime = 0.005;
    private readonly JobQueue _pulseJobQueue = new(PulseJobTime);

    public sealed class BlobPulse(
        BlobNodeSystem system,
        Entity<BlobNodeComponent> ent,
        double maxTime,
        CancellationToken cancellation = default)
        : Job<object>(maxTime, cancellation)
    {
        protected override async Task<object?> Process()
        {
            system.Pulse(ent);
            return null;
        }
    }

    private void OnTerminating(EntityUid uid, BlobNodeComponent component, ref EntityTerminatingEvent args)
    {
        OnDestruction(uid, component, new DestructionEventArgs());
    }

    private IEnumerable<Entity<BlobTileComponent>> GetSpecialBlobsTiles(BlobNodeComponent component)
    {
        if (!TerminatingOrDeleted(component.BlobFactory) && _tileQuery.TryComp(component.BlobFactory, out var tileFactoryComponent))
        {
            yield return (component.BlobFactory.Value, tileFactoryComponent);
        }
        if (!TerminatingOrDeleted(component.BlobResource) && _tileQuery.TryComp(component.BlobResource, out var tileResourceComponent))
        {
            yield return (component.BlobResource.Value, tileResourceComponent);
        }
    }

    private void OnDestruction(EntityUid uid, BlobNodeComponent component, DestructionEventArgs args)
    {
        if (!TryComp<BlobTileComponent>(uid, out var tileComp) ||
            tileComp.BlobTileType != BlobTileType.Node ||
            tileComp.Core == null)
            return;

        foreach (var tile in GetSpecialBlobsTiles(component))
        {
            tile.Comp.ReturnCost = false;
        }
    }

    private void Pulse(Entity<BlobNodeComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !EntityManager.TransformQuery.TryComp(ent, out var xform))
            return;

        var radius = ent.Comp.PulseRadius;

        var localPos = xform.Coordinates.Position;

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            return;
        }

        if (!_tileQuery.TryGetComponent(ent, out var blobTileComponent) || blobTileComponent.Core == null)
            return;

        var innerTiles = _map.GetLocalTilesIntersecting(xform.GridUid.Value,
                grid,
            new Box2(localPos + new Vector2(-radius, -radius), localPos + new Vector2(radius, radius)),
            false)
            .ToArray();

        _random.Shuffle(innerTiles);

        var explain = true;
        foreach (var tileRef in innerTiles)
        {
            foreach (var tile in _map.GetAnchoredEntities(xform.GridUid.Value, grid, tileRef.GridIndices))
            {
                if (!_tileQuery.HasComponent(tile))
                    continue;

                var ev = new BlobTileGetPulseEvent
                {
                    Handled = explain
                };
                RaiseLocalEvent(tile, ev);
                explain = false; // WTF?
            }
        }

        RaiseLocalEvent(ent, new BlobNodePulseEvent());
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _pulseJobQueue.Process();

        var blobNodeQuery = EntityQueryEnumerator<BlobNodeComponent, BlobTileComponent>();
        while (blobNodeQuery.MoveNext(out var ent, out var comp, out var blobTileComponent))
        {
            comp.NextPulse += frameTime;
            if (comp.PulseFrequency > comp.NextPulse)
                continue;

            comp.NextPulse -= comp.PulseFrequency;

            if (blobTileComponent.Core == null)
            {
                QueueDel(ent);
                continue;
            }
            _pulseJobQueue.EnqueueJob(new BlobPulse(this,(ent, comp), PulseJobTime));
        }
    }
}

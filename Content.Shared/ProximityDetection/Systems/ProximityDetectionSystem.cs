using Content.Shared.ProximityDetection.Components;
using Content.Shared.ProximityDetector;

namespace Content.Shared.ProximityDetection.Systems;


//This handles generic proximity detector logic
public sealed class ProximityDetectorSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ProximityDetectorComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<ProximityDetectorComponent, EntityUnpausedEvent>(OnUnpaused);
    }

    private void OnPaused(EntityUid uid, ProximityDetectorComponent component, EntityPausedEvent args)
    {
        SetEnable_Internal(component,false);
    }

    private void OnUnpaused(EntityUid uid, ProximityDetectorComponent component, ref EntityUnpausedEvent args)
    {
        SetEnable_Internal(component,true);
    }
    public void SetEnable(EntityUid uid, bool enabled, ProximityDetectorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        SetEnable_Internal( component, enabled);
    }

    private void SetEnable_Internal(ProximityDetectorComponent detector, bool enabled)
    {
        detector.Enabled = enabled;
        if (!enabled)
                detector.AccumulatedFrameTime = 0;
    }

    private void CheckDetection(Entity<ProximityDetectorComponent> detector)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(detector.Owner);
        List<(EntityUid TargetEnt, float Distance)> detections = new();
        foreach (var ent in _entityLookup.GetEntitiesInRange(_transform.GetMapCoordinates(detector, xform),
                     detector.Comp.MaximumDistance))
        {
            if (!detector.Comp.TargetRequirements.IsValid(ent, EntityManager))
                continue;
            var dist = (_transform.GetWorldPosition(xform, xformQuery) - _transform.GetWorldPosition(ent, xformQuery)).Length();
            var detectAttempt = new ProximityDetectionAttemptEvent(true, dist, detector);
            RaiseLocalEvent(ent, ref detectAttempt);
            if (detectAttempt.ShouldDetect)
            {
                detections.Add((ent,dist));
            }
        }
        if (detections.Count == 0)
        {
            var noDetectEvent = new ProximityDetectionNoTargetEvent();
            RaiseLocalEvent(detector, ref noDetectEvent);
            return;
        }
        var closestDistance = detections[0].Distance;
        EntityUid closestEnt = default!;
        foreach (var (ent,dist) in detections)
        {
            if (dist >= closestDistance)
                continue;
            closestEnt = ent;
            closestDistance = dist;
        }
        var detectEvent = new ProximityDetectionEvent(closestDistance, closestEnt);
        RaiseLocalEvent(detector, ref detectEvent);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ProximityDetectorComponent>();
        while (query.MoveNext(out var uid, out var detector))
        {
            if (!detector.Enabled)
                continue;
            detector.AccumulatedFrameTime += frameTime;
            if (detector.AccumulatedFrameTime < detector.UpdateRate)
                continue;
            detector.AccumulatedFrameTime -= detector.UpdateRate;
            CheckDetection((uid, detector));
        }
    }
}

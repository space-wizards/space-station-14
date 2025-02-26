using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.ProximityDetection.Components;
using Content.Shared.Tag;
using Robust.Shared.Network;

namespace Content.Shared.ProximityDetection.Systems;


//This handles generic proximity detector logic
public sealed class ProximityDetectionSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    // Hash sets of entities located in a main update loop.
    // Defined there to avoid additional allocations.
    private HashSet<Entity<IComponent>> _foundEnts = null!;
    private HashSet<Entity<IComponent>> _validEnts = new();

    //update is only run on the server

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ProximityDetectorComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ProximityDetectorComponent, ItemToggledEvent>(OnToggled);
    }

    private void OnCompInit(EntityUid uid, ProximityDetectorComponent component, ComponentInit args)
    {
        if (component.Criteria.RequireAll)
            return;
        Log.Debug("DetectorComponent only supports requireAll = false for tags. All components are required for a match!");
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<ProximityDetectorComponent>();
        while (query.MoveNext(out var owner, out var detector))
        {
            if (!_toggle.IsActivated(owner))
                continue;

            detector.AccumulatedFrameTime += frameTime;
            if (detector.AccumulatedFrameTime < detector.UpdateRate)
                continue;

            detector.AccumulatedFrameTime -= detector.UpdateRate;
            RunUpdate_Internal(owner, detector);
        }
    }

    private void OnToggled(Entity<ProximityDetectorComponent> ent, ref ItemToggledEvent args)
    {
        if (args.Activated)
        {
            RunUpdate_Internal(ent, ent.Comp);
            return;
        }

        var noDetectEvent = new ProximityTargetUpdatedEvent(ent.Comp, Target: null, ent.Comp.Distance);
        RaiseLocalEvent(ent, ref noDetectEvent);

        ent.Comp.AccumulatedFrameTime = 0;
        Dirty(ent, ent.Comp);
    }

    public void ForceUpdate(EntityUid owner, ProximityDetectorComponent? detector = null)
    {
        if (!Resolve(owner, ref detector))
            return;
        RunUpdate_Internal(owner, detector);
    }

    private void ClearTarget(Entity<ProximityDetectorComponent> ent)
    {
        var (uid, comp) = ent;
        if (comp.TargetEnt == null)
            return;

        comp.Distance = -1;
        comp.TargetEnt = null;
        var noDetectEvent = new ProximityTargetUpdatedEvent(comp, null, -1);
        RaiseLocalEvent(uid, ref noDetectEvent);
        var newTargetEvent = new NewProximityTargetEvent(comp, null);
        RaiseLocalEvent(uid, ref newTargetEvent);
        Dirty(uid, comp);
    }

    private void RunUpdate_Internal(EntityUid owner, ProximityDetectorComponent detector)
    {
        if (!_net.IsServer) //only run detection checks on the server!
            return;

        if (Deleted(detector.TargetEnt))
        {
            ClearTarget((owner, detector));
        }

        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(owner);
        List<(EntityUid TargetEnt, float Distance)> detections = new();

        if (detector.Criteria.Components == null)
        {
            Log.Error($"ProximityDetectorComponent on {ToPrettyString(owner)} must use at least 1 component as a filter in criteria!");
            throw new ArgumentException($"ProximityDetectorComponent on {ToPrettyString(owner)} must use at least 1 component as a filter in criteria!");
        }
        var tagSearchEnabled = detector.Criteria.Tags is {Count: > 0};

        FindWithMatchingComponents(out var foundEnts, owner, xform, detector, tagSearchEnabled);

        foreach (var ent in foundEnts)
        {
            if (tagSearchEnabled)
            {
                // Check for TagComponent present
                if (!TryComp<TagComponent>(ent, out var tags))
                    continue;
                // Check for tags present
                if (!(detector.Criteria.RequireAll
                    ? _tagSystem.HasAllTags(tags, detector.Criteria.Tags!)
                    : _tagSystem.HasAnyTag(tags, detector.Criteria.Tags!))
                )
                    continue;
            }

            var distance = (_transform.GetWorldPosition(xform, xformQuery) - _transform.GetWorldPosition(ent, xformQuery)).Length();
            if (CheckDetectConditions(ent, distance, owner, detector))
            {
                detections.Add((ent, distance));
            }
        }
        UpdateTargetFromClosest(owner, detector, detections);
    }
    private void FindWithMatchingComponents(
        out HashSet<Entity<IComponent>> foundEnts,
        EntityUid owner,
        TransformComponent xform,
        ProximityDetectorComponent detector,
        bool tagSearchEnabled
    )
    {
        foundEnts = new HashSet<Entity<IComponent>>();
        if (detector.Criteria.RequireAll)
        {
            var compType = EntityManager.ComponentFactory.GetRegistration(detector.Criteria.Components![0]).Type;
            foundEnts = _entityLookup.GetEntitiesInRange(compType, _transform.GetMapCoordinates(owner, xform), detector.Range.Float());
            _validEnts.EnsureCapacity(foundEnts.Count);
            for (var i = 1; i < detector.Criteria.Components!.Length; i++)
            {
                _validEnts.Clear();
                compType = EntityManager.ComponentFactory.GetRegistration(detector.Criteria.Components[i]).Type;
                foreach (var ent in foundEnts)
                {
                    if (!HasComp(ent, compType))
                        continue;
                    _validEnts.Add(ent);
                }
                (foundEnts, _validEnts) = (_validEnts, foundEnts);
            }
        }
        else
        {
            for (var i = 0; i < detector.Criteria.Components!.Length; i++)
            {
                var compType = EntityManager.ComponentFactory.GetRegistration(detector.Criteria.Components[i]).Type;
                foundEnts = _entityLookup.GetEntitiesInRange(compType, _transform.GetMapCoordinates(owner, xform), detector.Range.Float());
                _validEnts.UnionWith(foundEnts);
            }
            (foundEnts, _validEnts) = (_validEnts, foundEnts);
        }

        _validEnts.Clear();
        // If there is a need to match tags from search criteria,
        // TagComponent needs to exist, so we check for it.
        if (tagSearchEnabled)
        {
            foreach (var ent in foundEnts)
            {
                if (!HasComp<TagComponent>(ent))
                    continue;
                _validEnts.Add(ent);
            }
            (foundEnts, _validEnts) = (_validEnts, foundEnts);
            _validEnts.Clear();
        }

        return;
    }

    private bool CheckDetectConditions(EntityUid targetEntity, float dist, EntityUid owner, ProximityDetectorComponent detector)
    {
        var detectAttempt = new ProximityDetectionAttemptEvent(false, dist, (owner, detector));
        RaiseLocalEvent(targetEntity, ref detectAttempt);
        return !detectAttempt.Cancel;
    }

    private void UpdateTargetFromClosest(EntityUid owner, ProximityDetectorComponent detector, List<(EntityUid TargetEnt, float Distance)> detections)
    {
        if (detections.Count == 0)
        {
            ClearTarget((owner, detector));
            return;
        }
        var closestDistance = detections[0].Distance;
        EntityUid closestEnt = default!;
        foreach (var (ent, dist) in detections)
        {
            if (dist >= closestDistance)
                continue;
            closestEnt = ent;
            closestDistance = dist;
        }

        var newTarget = detector.TargetEnt != closestEnt;
        var newData = newTarget || detector.Distance != closestDistance;
        detector.TargetEnt = closestEnt;
        detector.Distance = closestDistance;
        Dirty(owner, detector);
        if (newTarget)
        {
            var newTargetEvent = new NewProximityTargetEvent(detector, closestEnt);
            RaiseLocalEvent(owner, ref newTargetEvent);
        }

        if (!newData)
            return;
        var targetUpdatedEvent = new ProximityTargetUpdatedEvent(detector, closestEnt, closestDistance);
        RaiseLocalEvent(owner, ref targetUpdatedEvent);
        Dirty(owner, detector);
    }

    public void SetRange(EntityUid owner, float newRange, ProximityDetectorComponent? detector = null)
    {
        if (!Resolve(owner, ref detector))
            return;
        detector.Range = newRange;
        Dirty(owner, detector);
    }
}

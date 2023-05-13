using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
    }

    private void OnActivate(EntityUid uid, PinpointerComponent component, ActivateInWorldEvent args)
    {
        TogglePinpointer(uid, component);
        LocateTarget(uid, component);
    }

    public bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return false;

        var isActive = !pinpointer.IsActive;
        SetActive(uid, isActive, pinpointer);
        UpdateAppearance(uid, pinpointer);
        return isActive;
    }

    private void UpdateAppearance(EntityUid uid, PinpointerComponent pinpointer, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref appearance))
            return;
        _appearance.SetData(uid, PinpointerVisuals.IsActive, pinpointer.IsActive, appearance);
        _appearance.SetData(uid, PinpointerVisuals.TargetDistance, pinpointer.DistanceToTarget, appearance);
    }

    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            LocateTarget(uid, pinpointer);
        }
    }

    private void LocateTarget(EntityUid uid, PinpointerComponent component)
    {
        // try to find target from whitelist
        if (component.IsActive && component.Component != null)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component.Component, out var reg))
            {
                Logger.Error($"Unable to find component registration for {component.Component} for pinpointer!");
                DebugTools.Assert(false);
                return;
            }

            var target = FindTargetFromComponent(uid, reg.Type);
            SetTarget(uid, target, component);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // because target or pinpointer can move
        // we need to update pinpointers arrow each frame
        var query = EntityQueryEnumerator<PinpointerComponent>();
        while (query.MoveNext(out var uid, out var pinpointer))
        {
            UpdateDirectionToTarget(uid, pinpointer);
        }
    }

    /// <summary>
    ///     Try to find the closest entity from whitelist on a current map
    ///     Will return null if can't find anything
    /// </summary>
    private EntityUid? FindTargetFromComponent(EntityUid uid, Type whitelist, TransformComponent? transform = null)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        if (transform == null)
            xformQuery.TryGetComponent(uid, out transform);

        if (transform == null)
            return null;

        // sort all entities in distance increasing order
        var mapId = transform.MapID;
        var l = new SortedList<float, EntityUid>();
        var worldPos = _transform.GetWorldPosition(transform, xformQuery);

        foreach (var comp in EntityManager.GetAllComponents(whitelist))
        {
            if (!xformQuery.TryGetComponent(comp.Owner, out var compXform) || compXform.MapID != mapId)
                continue;

            var dist = (_transform.GetWorldPosition(compXform, xformQuery) - worldPos).LengthSquared;
            l.TryAdd(dist, comp.Owner);
        }

        // return uid with a smallest distance
        return l.Count > 0 ? l.First().Value : null;
    }

    /// <summary>
    ///     Set pinpointers target to track
    /// </summary>
    public void SetTarget(EntityUid uid, EntityUid? target, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (pinpointer.Target == target)
            return;

        pinpointer.Target = target;
        if (pinpointer.IsActive)
            UpdateDirectionToTarget(uid, pinpointer);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    private void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent pinpointer)
    {
        if (!pinpointer.IsActive)
            return;

        var target = pinpointer.Target;
        if (target == null || !EntityManager.EntityExists(target.Value))
        {
            SetDistance(uid, Distance.Unknown, pinpointer);
            return;
        }

        var dirVec = CalculateDirection(uid, target.Value);
        var oldDist = pinpointer.DistanceToTarget;
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(uid, angle, pinpointer);
            var dist = CalculateDistance(dirVec.Value, pinpointer);
            SetDistance(uid, dist, pinpointer);
        }
        else
        {
            SetDistance(uid, Distance.Unknown, pinpointer);
        }
        if (oldDist != pinpointer.DistanceToTarget)
            UpdateAppearance(uid, pinpointer);
    }

    /// <summary>
    ///     Calculate direction from pinUid to trgUid
    /// </summary>
    /// <returns>Null if failed to calculate distance between two entities</returns>
    private Vector2? CalculateDirection(EntityUid pinUid, EntityUid trgUid)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();

        // check if entities have transform component
        if (!xformQuery.TryGetComponent(pinUid, out var pin))
            return null;
        if (!xformQuery.TryGetComponent(trgUid, out var trg))
            return null;

        // check if they are on same map
        if (pin.MapID != trg.MapID)
            return null;

        // get world direction vector
        var dir = _transform.GetWorldPosition(trg, xformQuery) - _transform.GetWorldPosition(pin, xformQuery);
        return dir;
    }

    private static Distance CalculateDistance(Vector2 vec, PinpointerComponent pinpointer)
    {
        var dist = vec.Length;
        if (dist <= pinpointer.ReachedDistance)
            return Distance.Reached;
        else if (dist <= pinpointer.CloseDistance)
            return Distance.Close;
        else if (dist <= pinpointer.MediumDistance)
            return Distance.Medium;
        else
            return Distance.Far;
    }
}

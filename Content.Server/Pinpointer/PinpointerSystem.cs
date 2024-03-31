using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Maps.Components;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Content.Server.Respawn;
using Content.Server.Shuttles.Events;

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPausedMapStorageSystem _pausedMapStorage = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
        SubscribeLocalEvent<BeforeEnterPausedMapEvent>(OnBeforeEnterPausedMap);
        SubscribeLocalEvent<AfterExitPausedMapEvent>(OnAfterExitPausedMap);
        SubscribeLocalEvent<SpecialRespawnEvent>(OnSpecialRespawn);
    }

    public override bool TogglePinpointer(EntityUid uid, PinpointerComponent? pinpointer = null)
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
        Dirty(uid, pinpointer);
    }

    private void OnActivate(EntityUid uid, PinpointerComponent component, ActivateInWorldEvent args)
    {
        TogglePinpointer(uid, component);

        if (!component.CanRetarget)
            LocateTarget(uid, component);
    }

    private void OnLocateTarget(ref FTLCompletedEvent args)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer.CanRetarget)
                continue;

            LocateTarget(uid, pinpointer);
        }
    }

    private void OnBeforeEnterPausedMap(ref BeforeEnterPausedMapEvent args)
    {
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
            if (pinpointer.Target == args.Entity)
                SetTarget(uid, args.Proxy, pinpointer);
    }

    private void OnAfterExitPausedMap(ref AfterExitPausedMapEvent args)
    {
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer == null || pinpointer.Target != args.Proxy)
                continue;

            if (string.IsNullOrEmpty(pinpointer!.Component) || !EntityManager.ComponentFactory.TryGetRegistration(pinpointer!.Component, out var reg))
            {
                Log.Error($"Unable to find component registration for {pinpointer.Component} for pinpointer!");
                DebugTools.Assert(false);
                continue;
            }

            if (!HasComp(args.Entity, reg.Type))
                continue;

            SetTarget(uid, args.Entity, pinpointer);
        }
    }

    private void OnSpecialRespawn(ref SpecialRespawnEvent args)
    {
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
            if (pinpointer.Target == args.OldEntity)
                SetTarget(uid, args.NewEntity, pinpointer);
    }

    private void LocateTarget(EntityUid uid, PinpointerComponent component)
    {
        // try to find target from whitelist
        if (component.IsActive && component.Component != null)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(component.Component, out var reg))
            {
                Log.Error($"Unable to find component registration for {component.Component} for pinpointer!");
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
    ///     Raises <see cref="EventNameHere"/> to look for entities that make use of paused maps
    ///     Will return null if can't find anything
    /// </summary>
    private EntityUid? FindTargetFromComponent(EntityUid uid, Type whitelist, TransformComponent? transform = null)
    {
        _xformQuery.Resolve(uid, ref transform, false);

        if (transform == null)
            return null;

        // sort all entities in distance increasing order
        var mapId = transform.MapID;
        var worldPos = _transform.GetWorldPosition(transform);
        var sortedList = new SortedList<float, EntityUid>();

        foreach (var (otherUid, _) in EntityManager.GetAllComponents(whitelist, true))
        {
            if (!_xformQuery.TryGetComponent(otherUid, out var otherXform))
                continue;

            var result = new Entity<TransformComponent>(otherUid, otherXform);

            if (otherXform.MapID != mapId)
            {
                if (!TryComp<SharedPausedMapStorageComponent>(otherUid, out var otherStorage))
                    continue;

                if (!_pausedMapStorage.IsInPausedMap(otherUid))
                    continue;

                if (Deleted(otherStorage.Proxy))
                    continue;

                result = (otherStorage.Proxy, Transform(otherStorage.Proxy));
            }

            var dist = (_transform.GetWorldPosition(result.Comp) - worldPos).LengthSquared();
            sortedList.TryAdd(dist, result.Owner);
        }

        // return uid with a smallest distance
        return sortedList.Count > 0 ? sortedList.First().Value : null;
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected override void UpdateDirectionToTarget(EntityUid uid, PinpointerComponent? pinpointer = null)
    {
        if (!Resolve(uid, ref pinpointer))
            return;

        if (!pinpointer.IsActive)
            return;

        var target = pinpointer.Target;
        if (target == null || !EntityManager.EntityExists(target.Value))
        {
            if (TrySetArrowAngle(uid, Angle.Zero, pinpointer) || TrySetDistance(uid, Distance.Unknown, pinpointer))
                UpdateAppearance(uid, pinpointer);

            return;
        }

        var dirVec = CalculateDirection(uid, target.Value);
        if (dirVec == null)
        {
            if (TrySetArrowAngle(uid, Angle.Zero, pinpointer) || TrySetDistance(uid, Distance.Unknown, pinpointer))
                UpdateAppearance(uid, pinpointer);

            return;
        }

        var angle = dirVec.Value.ToWorldAngle();
        var dist = CalculateDistance(dirVec.Value, pinpointer);

        if (TrySetArrowAngle(uid, angle, pinpointer) || TrySetDistance(uid, dist, pinpointer))
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

    private Distance CalculateDistance(Vector2 vec, PinpointerComponent pinpointer)
    {
        var dist = vec.Length();
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

using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.Pinpointer;

public sealed class PinpointerSystem : SharedPinpointerSystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<PinpointerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<FTLCompletedEvent>(OnLocateTarget);
    }

    public override bool TogglePinpointer(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var isActive = !ent.Comp.IsActive;
        SetActive(ent, isActive);
        UpdateAppearance(ent);
        return isActive;
    }

    private void UpdateAppearance(Entity<PinpointerComponent?, AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1) || !Resolve(ent, ref ent.Comp2))
            return;

        _appearance.SetData(ent, PinpointerVisuals.IsActive, ent.Comp1.IsActive, ent.Comp2);
        _appearance.SetData(ent, PinpointerVisuals.TargetDistance, ent.Comp1.DistanceToTarget, ent.Comp2);
    }

    private void OnActivate(Entity<PinpointerComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        TogglePinpointer(ent.AsNullable());

        // We update the target entity in case a closer entity with the right constraints appears
        UpdateTargetEntity(ent.AsNullable());

        args.Handled = true;
    }

    /// <summary>
    ///     Refreshes the entity target of all pinpointers when an FTL jump happens.
    /// </summary>
    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This is necessary due to pinpointers only looking for entities within the current map.
        // Otherwise, players will have to manually toggle their pinpointers off and on again every time they FTL jump to refresh them.
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer.CanRetarget)
                continue;

            UpdateTargetEntity((uid,pinpointer));
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
            UpdateDirectionToTarget((uid, pinpointer));
        }
    }

    protected override void UpdateTargetEntity(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var target = LocateTarget(ent, ent.Comp.Target);

        if (ent.Comp.TargetEntity == target)
            return;

        ent.Comp.TargetEntity = target;
        if (ent.Comp.IsActive)
            UpdateDirectionToTarget(ent);
    }

    /// <summary>
    ///     Update direction from pinpointer to selected target (if it was set)
    /// </summary>
    protected override void UpdateDirectionToTarget(Entity<PinpointerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var pinpointer = ent.Comp;

        if (!pinpointer.IsActive)
            return;

        var target = pinpointer.TargetEntity;
        if (target == null || !Exists(target.Value))
        {
            SetDistance(ent, Distance.Unknown);
            return;
        }

        var dirVec = CalculateDirection(ent, target.Value);
        var oldDist = pinpointer.DistanceToTarget;
        if (dirVec != null)
        {
            var angle = dirVec.Value.ToWorldAngle();
            TrySetArrowAngle(ent, angle);
            var dist = CalculateDistance(dirVec.Value, pinpointer);
            SetDistance(ent, dist);
        }
        else
        {
            SetDistance(ent, Distance.Unknown);
        }
        if (oldDist != pinpointer.DistanceToTarget)
            UpdateAppearance(ent);
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

    /// <summary>
    ///     Try to find the closest entity from whitelist on a current map
    ///     Will return null if can't find anything
    /// </summary>
    private EntityUid? FindTargetFromComponent(Entity<TransformComponent?> ent, Type whitelist, EntProtoId? protoId = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        // sort all entities in distance increasing order
        var mapId = ent.Comp.MapID;
        var l = new SortedList<float, EntityUid>();
        var worldPos = _transform.GetWorldPosition(ent.Comp);

        foreach (var (otherUid, _) in EntityManager.GetAllComponents(whitelist))
        {
            if (!_xformQuery.TryGetComponent(otherUid, out var compXform) || compXform.MapID != mapId)
                continue;

            // If no protoId is specified, don't filter for entities with a specific proto id.
            if (protoId is not null && Prototype(otherUid)?.ID != protoId)
                continue;

            var dist = (_transform.GetWorldPosition(compXform) - worldPos).LengthSquared();
            l.TryAdd(dist, otherUid);
        }

        // return uid with a smallest distance
        return l.Count > 0 ? l.First().Value : null;
    }

    /// <summary>
    ///     Gets an EntityUid target from a PinpointerTarget, if the target exists. Entity is what is being
    ///     used to calculate the closest entity with a given component, so Entity should almost always be
    ///     the pinpointer.
    /// </summary>
    /// <remarks>
    ///     Add a new case here if you are creating a new subclass of <see cref="PinpointerTarget"/>.
    /// </remarks>
    private EntityUid? LocateTarget(EntityUid entity, PinpointerTarget? target)
    {
        EntityUid? result = null;
        if (target is null)
        {
            return result;
        }

        switch (target)
        {
            case PinpointerComponentTarget component:
            {
                if (!EntityManager.ComponentFactory.TryGetRegistration(component.Target, out var reg))
                {
                    Log.Error($"Unable to find component registration for {component.Target} for pinpointer!");
                    DebugTools.Assert(false);
                    break;
                }

                if (!_xformQuery.TryComp(entity, out var transform))
                    break;

                // There may be multiple entities with the specified component, so we want the closest one at the time of activation
                result = FindTargetFromComponent((entity, transform), reg.Type);
                break;
            }
            case PinpointerEntityUidTarget entityUid:
            {
                result = entityUid.Target;
                break;
            }
            case PinpointerEntProtoIdTarget entProtoId:
            {
                if (!EntityManager.ComponentFactory.TryGetRegistration(entProtoId.Component, out var reg))
                {
                    Log.Error($"Unable to find component registration for {entProtoId.Component} for pinpointer!");
                    DebugTools.Assert(false);
                    break;
                }

                if (!_xformQuery.TryComp(entity, out var transform))
                    break;

                result = FindTargetFromComponent((entity, transform), reg.Type, entProtoId.Target);
                break;
            }
            default:
            {
                Log.Error("Invalid PinpointerTarget record");
                DebugTools.Assert(false);
                break;
            }
        }

        return result;
    }
}

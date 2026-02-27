using Content.Shared.Interaction;
using Content.Shared.Pinpointer;
using System.Linq;
using System.Numerics;
using Robust.Shared.Utility;
using Content.Server.Shuttles.Events;

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

        if (!ent.Comp.CanRetarget)
            LocateTarget(ent);

        args.Handled = true;
    }

    private void OnLocateTarget(ref FTLCompletedEvent ev)
    {
        // This feels kind of expensive, but it only happens once per hyperspace jump

        // todo: ideally, you would need to raise this event only on jumped entities
        // this code update ALL pinpointers in game
        var query = EntityQueryEnumerator<PinpointerComponent>();

        while (query.MoveNext(out var uid, out var pinpointer))
        {
            if (pinpointer.CanRetarget)
                continue;

            LocateTarget((uid, pinpointer));
        }
    }

    private void LocateTarget(Entity<PinpointerComponent> ent)
    {
        // try to find target from whitelist
        if (ent.Comp.IsActive && ent.Comp.Component != null)
        {
            if (!EntityManager.ComponentFactory.TryGetRegistration(ent.Comp.Component, out var reg))
            {
                Log.Error($"Unable to find component registration for {ent.Comp.Component} for pinpointer!");
                DebugTools.Assert(false);
                return;
            }

            var target = FindTargetFromComponent(ent.Owner, reg.Type);
            SetTarget(ent.AsNullable(), target);
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

        var target = pinpointer.Target;
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
}

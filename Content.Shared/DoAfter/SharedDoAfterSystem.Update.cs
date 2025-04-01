using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Shared.DoAfter;

public abstract partial class SharedDoAfterSystem : EntitySystem
{
    [Dependency] private readonly IDynamicTypeFactory _factory = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private DoAfter[] _doAfters = Array.Empty<DoAfter>();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = GameTiming.CurTime;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var handsQuery = GetEntityQuery<HandsComponent>();

        var enumerator = EntityQueryEnumerator<ActiveDoAfterComponent, DoAfterComponent>();
        while (enumerator.MoveNext(out var uid, out var active, out var comp))
        {
            Update(uid, active, comp, time, xformQuery, handsQuery);
        }
    }

    protected void Update(
        EntityUid uid,
        ActiveDoAfterComponent active,
        DoAfterComponent comp,
        TimeSpan time,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<HandsComponent> handsQuery)
    {
        var dirty = false;

        var values = comp.DoAfters.Values;
        var count = values.Count;
        if (_doAfters.Length < count)
            _doAfters = new DoAfter[count];

        values.CopyTo(_doAfters, 0);
        for (var i = 0; i < count; i++)
        {
            var doAfter = _doAfters[i];
            if (doAfter.CancelledTime != null)
            {
                if (time - doAfter.CancelledTime.Value > ExcessTime)
                {
                    comp.DoAfters.Remove(doAfter.Index);
                    dirty = true;
                }
                continue;
            }

            if (doAfter.Completed)
            {
                if (time - doAfter.StartTime > doAfter.Args.Delay + ExcessTime)
                {
                    comp.DoAfters.Remove(doAfter.Index);
                    dirty = true;
                }
                continue;
            }

            if (ShouldCancel(doAfter, xformQuery, handsQuery))
            {
                InternalCancel(doAfter, comp);
                dirty = true;
                continue;
            }

            if (time - doAfter.StartTime >= doAfter.Args.Delay)
            {
                TryComplete(doAfter, comp);
                dirty = true;
            }
        }

        if (dirty)
            Dirty(uid, comp);

        if (comp.DoAfters.Count == 0)
            RemCompDeferred(uid, active);
    }

    private bool TryAttemptEvent(DoAfter doAfter)
    {
        var args = doAfter.Args;

        if (args.ExtraCheck?.Invoke() == false)
            return false;

        if (doAfter.AttemptEvent == null)
        {
            // I feel like this is somewhat cursed, but its the only way I can think of without having to just send
            // redundant data over the network and increasing DoAfter boilerplate.
            var evType = typeof(DoAfterAttemptEvent<>).MakeGenericType(args.Event.GetType());
            doAfter.AttemptEvent = _factory.CreateInstance(evType, new object[] { doAfter, args.Event });
        }

        args.Event.DoAfter = doAfter;
        if (args.EventTarget != null)
            RaiseLocalEvent(args.EventTarget.Value, doAfter.AttemptEvent, args.Broadcast);
        else
            RaiseLocalEvent(doAfter.AttemptEvent);

        var ev = (CancellableEntityEventArgs) doAfter.AttemptEvent;
        if (!ev.Cancelled)
            return true;

        ev.Uncancel();
        return false;
    }

    private void TryComplete(DoAfter doAfter, DoAfterComponent component)
    {
        if (doAfter.Cancelled || doAfter.Completed)
            return;

        // Perform final check (if required)
        if (doAfter.Args.AttemptFrequency == AttemptFrequency.StartAndEnd
            && !TryAttemptEvent(doAfter))
        {
            InternalCancel(doAfter, component);
            return;
        }

        doAfter.Completed = true;

        RaiseDoAfterEvents(doAfter, component);

        if (doAfter.Args.Event.Repeat)
        {
            doAfter.StartTime = GameTiming.CurTime;
            doAfter.Completed = false;
        }
    }

    private bool ShouldCancel(DoAfter doAfter,
        EntityQuery<TransformComponent> xformQuery,
        EntityQuery<HandsComponent> handsQuery)
    {
        var args = doAfter.Args;

        //re-using xformQuery for Exists() checks.
        if (args.Used is { } used && !xformQuery.HasComponent(used))
            return true;

        if (args.EventTarget is {Valid: true} eventTarget && !xformQuery.HasComponent(eventTarget))
            return true;

        if (!xformQuery.TryGetComponent(args.User, out var userXform))
            return true;

        TransformComponent? targetXform = null;
        if (args.Target is { } target && !xformQuery.TryGetComponent(target, out targetXform))
            return true;

        TransformComponent? usedXform = null;
        if (args.Used is { } @using && !xformQuery.TryGetComponent(@using, out usedXform))
            return true;

        // TODO: Re-use existing xform query for these calculations.
        if (args.BreakOnMove && !(!args.BreakOnWeightlessMove && _gravity.IsWeightless(args.User, xform: userXform)))
        {
            // Whether the user has moved too much from their original position.
            if (!_transform.InRange(userXform.Coordinates, doAfter.UserPosition, args.MovementThreshold))
                return true;

            // Whether the distance between the user and target(if any) has changed too much.
            if (targetXform != null &&
                targetXform.Coordinates.TryDistance(EntityManager, userXform.Coordinates, out var distance))
            {
                if (Math.Abs(distance - doAfter.TargetDistance) > args.MovementThreshold)
                    return true;
            }
        }

        // Whether the user and the target are too far apart.
        if (args.Target != null)
        {
            if (args.DistanceThreshold != null)
            {
                if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value, args.DistanceThreshold.Value))
                    return true;
            }
            else
            {
                if (!_interaction.InRangeUnobstructed(args.User, args.Target.Value))
                    return true;
            }
        }

        // Whether the distance between the tool and the user has grown too much.
        if (args.Used != null)
        {
            if (args.DistanceThreshold != null)
            {
                if (!_interaction.InRangeUnobstructed(args.User,
                        args.Used.Value,
                        args.DistanceThreshold.Value))
                    return true;
            }
            else
            {
                if (!_interaction.InRangeUnobstructed(args.User,args.Used.Value))
                    return true;
            }
        }

        if (args.AttemptFrequency == AttemptFrequency.EveryTick && !TryAttemptEvent(doAfter))
            return true;

        // Check if the do-after requires hands to perform at first
        // For example, you need hands to strip clothes off of someone
        // This does not mean their hand needs to be empty.
        if (args.NeedHand)
        {
            if (!handsQuery.TryGetComponent(args.User, out var hands) || hands.Count == 0)
                return true;

            // If an item was in the user's hand to begin with,
            // check if the user is no longer holding the item.
            if (args.BreakOnDropItem && doAfter.InitialItem != null && !_hands.IsHolding((args.User, hands), doAfter.InitialItem))
                    return true;

            // If the user changes which hand is active at all, interrupt the do-after
            if (args.BreakOnHandChange && hands.ActiveHand?.Name != doAfter.InitialHand)
                return true;
        }

        if (args.RequireCanInteract && !_actionBlocker.CanInteract(args.User, args.Target))
            return true;


        return false;
    }
}

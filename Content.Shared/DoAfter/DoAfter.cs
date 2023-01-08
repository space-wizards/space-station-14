using System.Threading.Tasks;
using Content.Shared.Hands.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.DoAfter;
[Serializable, NetSerializable]
public sealed class DoAfter
{
    [NonSerialized]
    public Task<DoAfterStatus> AsTask;

    [NonSerialized]
    [Obsolete("Will be obsolete for EventBus")]
    public TaskCompletionSource<DoAfterStatus> Tcs;

    public readonly DoAfterEventArgs EventArgs;

    //ID so the client DoAfterSystem can track
    public byte ID;

    public bool Cancelled = false;

    //Cache the delay so the timer properly shows
    public float Delay;

    [NonSerialized]
    private readonly IGameTiming _gameTiming;

    //Keep track of the time this DoAfter started
    public TimeSpan StartTime;

    //How long has the do after been running?
    public TimeSpan Elapsed = TimeSpan.Zero;

    /// <summary>
    /// Accrued time when cancelled.
    /// </summary>
    public TimeSpan CancelledElapsed;

    public EntityCoordinates UserGrid;
    public EntityCoordinates TargetGrid;

#pragma warning disable RA0004
    public DoAfterStatus Status => AsTask.IsCompletedSuccessfully ? AsTask.Result : DoAfterStatus.Running;
#pragma warning restore RA0004

    // NeedHand
    public readonly string? ActiveHand;
    public readonly EntityUid? ActiveItem;

    public DoAfter(DoAfterEventArgs eventArgs, IEntityManager entityManager)
    {
        EventArgs = eventArgs;
        _gameTiming = IoCManager.Resolve<IGameTiming>();
        StartTime = _gameTiming.CurTime;

        if (eventArgs.BreakOnUserMove)
            UserGrid = entityManager.GetComponent<TransformComponent>(eventArgs.User).Coordinates;

        if (eventArgs.Target != null && eventArgs.BreakOnTargetMove)
            // Target should never be null if the bool is set.
            TargetGrid = entityManager.GetComponent<TransformComponent>(eventArgs.Target!.Value).Coordinates;

        // For this we need to stay on the same hand slot and need the same item in that hand slot
        // (or if there is no item there we need to keep it free).
        if (eventArgs.NeedHand && entityManager.TryGetComponent(eventArgs.User, out SharedHandsComponent? handsComponent))
        {
            ActiveHand = handsComponent.ActiveHand?.Name;
            ActiveItem = handsComponent.ActiveHandEntity;
        }

        Tcs = new TaskCompletionSource<DoAfterStatus>();
        AsTask = Tcs.Task;
    }

    [Obsolete("Use SharedDoAfterSystem.Cancel instead")]
    public void Cancel()
    {
        if (Status == DoAfterStatus.Running)
            Tcs.SetResult(DoAfterStatus.Cancelled);
    }

    [Obsolete("Use SharedDoAfterSystem.Run instead")]
    public void Run(IEntityManager entityManager)
    {
        switch (Status)
        {
            case DoAfterStatus.Running:
                break;
            case DoAfterStatus.Cancelled:
            case DoAfterStatus.Finished:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Elapsed = _gameTiming.CurTime - StartTime;

        if (IsFinished())
        {
            // Do the final checks here
            if (!TryPostCheck())
                Tcs.SetResult(DoAfterStatus.Cancelled);
            else
                Tcs.SetResult(DoAfterStatus.Finished);
            return;
        }

        if (IsCancelled(entityManager))
            Tcs.SetResult(DoAfterStatus.Cancelled);
    }

    [Obsolete("Use SharedDoAfterSystem.IsCancelled instead")]
    private bool IsCancelled(IEntityManager entityManager)
    {
        if (!entityManager.EntityExists(EventArgs.User) || EventArgs.Target is {} target && !entityManager.EntityExists(target))
            return true;

        //https://github.com/tgstation/tgstation/blob/1aa293ea337283a0191140a878eeba319221e5df/code/__HELPERS/mobs.dm
        if (EventArgs.CancelToken.IsCancellationRequested)
            return true;

        // TODO :Handle inertia in space.
        if (EventArgs.BreakOnUserMove && !entityManager.GetComponent<TransformComponent>(EventArgs.User).Coordinates.InRange(entityManager, UserGrid, EventArgs.MovementThreshold))
            return true;

        if (EventArgs.Target != null &&
            EventArgs.BreakOnTargetMove &&
            !entityManager.GetComponent<TransformComponent>(EventArgs.Target!.Value).Coordinates.InRange(entityManager, TargetGrid, EventArgs.MovementThreshold))
        {
            return true;
        }

        if (EventArgs.ExtraCheck != null && !EventArgs.ExtraCheck.Invoke())
            return true;

        if (EventArgs.BreakOnStun && entityManager.HasComponent<StunnedComponent>(EventArgs.User))
            return true;

        if (EventArgs.NeedHand)
        {
            if (!entityManager.TryGetComponent(EventArgs.User, out SharedHandsComponent? handsComponent))
            {
                // If we had a hand but no longer have it that's still a paddlin'
                if (ActiveHand != null)
                    return true;
            }
            else
            {
                var currentActiveHand = handsComponent.ActiveHand?.Name;
                if (ActiveHand != currentActiveHand)
                    return true;

                var currentItem = handsComponent.ActiveHandEntity;
                if (ActiveItem != currentItem)
                    return true;
            }
        }

        if (EventArgs.DistanceThreshold != null)
        {
            var xformQuery = entityManager.GetEntityQuery<TransformComponent>();
            TransformComponent? userXform = null;

            // Check user distance to target AND used entities.
            if (EventArgs.Target != null && !EventArgs.User.Equals(EventArgs.Target))
            {
                //recalculate Target location in case Target has also moved
                var targetCoordinates = xformQuery.GetComponent(EventArgs.Target.Value).Coordinates;
                userXform ??= xformQuery.GetComponent(EventArgs.User);
                if (!userXform.Coordinates.InRange(entityManager, targetCoordinates, EventArgs.DistanceThreshold.Value))
                    return true;
            }

            if (EventArgs.Used != null)
            {
                var targetCoordinates = xformQuery.GetComponent(EventArgs.Used.Value).Coordinates;
                userXform ??= xformQuery.GetComponent(EventArgs.User);
                if (!userXform.Coordinates.InRange(entityManager, targetCoordinates, EventArgs.DistanceThreshold.Value))
                    return true;
            }
        }

        return false;
    }

    [Obsolete("Use SharedDoAfterSystem.TryPostCheck instead")]
    private bool TryPostCheck()
    {
        return EventArgs.PostCheck?.Invoke() != false;
    }

    [Obsolete("Use SharedDoAfterSystem.IsFinished instead")]
    private bool IsFinished()
    {
        var delay = TimeSpan.FromSeconds(EventArgs.Delay);

        if (Elapsed <= delay)
            return false;

        return true;
    }
}

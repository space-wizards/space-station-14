using System.Threading.Tasks;
using Content.Shared.Hands.Components;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.DoAfter;
[Serializable, NetSerializable]
[DataDefinition]
public sealed class DoAfter
{
    [NonSerialized]
    [Obsolete]
    public Task<DoAfterStatus> AsTask;

    [NonSerialized]
    [Obsolete("Will be obsolete for EventBus")]
    public TaskCompletionSource<DoAfterStatus> Tcs;

    //TODO: Should be merged into here
    public readonly DoAfterEventArgs EventArgs;

    //ID so the client DoAfterSystem can track
    public byte ID;

    public bool Cancelled = false;

    //Cache the delay so the timer properly shows
    public float Delay;

    //Keep track of the time this DoAfter started
    public TimeSpan StartTime;

    //Keep track of the time this DoAfter was cancelled
    public TimeSpan CancelledTime;

    //How long has the do after been running?
    public TimeSpan Elapsed = TimeSpan.Zero;

    /// <summary>
    /// Accrued time when cancelled.
    /// </summary>
    public TimeSpan CancelledElapsed = TimeSpan.Zero;

    public EntityCoordinates UserGrid;
    public EntityCoordinates TargetGrid;

    [NonSerialized]
    public Action<bool>? Done;

#pragma warning disable RA0004
    public DoAfterStatus Status => AsTask.IsCompletedSuccessfully ? AsTask.Result : DoAfterStatus.Running;
#pragma warning restore RA0004

    // NeedHand
    public readonly string? ActiveHand;
    public readonly EntityUid? ActiveItem;

    public DoAfter(DoAfterEventArgs eventArgs, IEntityManager entityManager)
    {
        EventArgs = eventArgs;
        StartTime = IoCManager.Resolve<IGameTiming>().CurTime;

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
}

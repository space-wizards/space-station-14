using Robust.Shared.Serialization;
using System.Numerics;

namespace Content.Shared.Pointing;

public abstract class SharedPointingSystem : EntitySystem
{
    protected readonly TimeSpan PointDuration = TimeSpan.FromSeconds(4);
    protected readonly float PointKeyTimeMove = 0.1f;
    protected readonly float PointKeyTimeHover = 0.5f;

    [Serializable, NetSerializable]
    public sealed class SharedPointingArrowComponentState : ComponentState
    {
        public Vector2 StartPosition { get; init; }
        public TimeSpan EndTime { get; init; }
    }

    public bool CanPoint(EntityUid uid)
    {
        var ev = new PointAttemptEvent(uid);
        RaiseLocalEvent(uid, ev, true);

        return !ev.Cancelled;
    }
}

public sealed class PointAttemptEvent : CancellableEntityEventArgs
{
    public PointAttemptEvent(EntityUid uid)
    {
        Uid = uid;
    }

    public EntityUid Uid { get; }
}

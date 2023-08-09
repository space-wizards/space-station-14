using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter
{
    [NetworkedComponent()]
    public abstract class SharedDoAfterComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class DoAfterComponentState : ComponentState
    {
        public List<ClientDoAfter> DoAfters { get; }

        public DoAfterComponentState(List<ClientDoAfter> doAfters)
        {
            DoAfters = doAfters;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CancelledDoAfterMessage : EntityEventArgs
    {
        public EntityUid Uid;
        public byte ID { get; }

        public CancelledDoAfterMessage(EntityUid uid, byte id)
        {
            Uid = uid;
            ID = id;
        }
    }

    // TODO: Merge this with the actual DoAfter
    /// <summary>
    ///     We send a trimmed-down version of the DoAfter for the client for it to use.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ClientDoAfter
    {
        public bool Cancelled = false;

        /// <summary>
        /// Accrued time when cancelled.
        /// </summary>
        public float CancelledAccumulator;

        // To see what these do look at DoAfter and DoAfterEventArgs
        public byte ID { get; }

        public TimeSpan StartTime { get; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        public EntityUid? Target { get; }

        public float Accumulator;

        public float Delay { get; }

        // TODO: The other ones need predicting
        public bool BreakOnUserMove { get; }

        public bool BreakOnTargetMove { get; }

        public float MovementThreshold { get; }

        public FixedPoint2 DamageThreshold { get; }

        public ClientDoAfter(byte id, EntityCoordinates userGrid, EntityCoordinates targetGrid, TimeSpan startTime,
            float delay, bool breakOnUserMove, bool breakOnTargetMove, float movementThreshold, FixedPoint2 damageThreshold, EntityUid? target = null)
        {
            ID = id;
            UserGrid = userGrid;
            TargetGrid = targetGrid;
            StartTime = startTime;
            Delay = delay;
            BreakOnUserMove = breakOnUserMove;
            BreakOnTargetMove = breakOnTargetMove;
            MovementThreshold = movementThreshold;
            DamageThreshold = damageThreshold;
            Target = target;
        }
    }
}

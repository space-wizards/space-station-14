using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedDoAfterComponent : Component
    {
        public override string Name => "DoAfter";

        public override uint? NetID => ContentNetIDs.DO_AFTER;
    }

    [Serializable, NetSerializable]
    public sealed class CancelledDoAfterMessage : ComponentMessage
    {
        public byte ID { get; }

        public CancelledDoAfterMessage(byte id)
        {
            ID = id;
        }
    }

    /// <summary>
    ///     We send a trimmed-down version of the DoAfter for the client for it to use.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DoAfterMessage : ComponentMessage
    {
        // To see what these do look at DoAfter and DoAfterEventArgs
        public byte ID { get; }

        public TimeSpan StartTime { get; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        public EntityUid TargetUid { get; }

        public float Delay { get; }

        // TODO: The other ones need predicting
        public bool BreakOnUserMove { get; }

        public bool BreakOnTargetMove { get; }

        public DoAfterMessage(byte id, EntityCoordinates userGrid, EntityCoordinates targetGrid, TimeSpan startTime, float delay, bool breakOnUserMove, bool breakOnTargetMove, EntityUid targetUid = default)
        {
            ID = id;
            UserGrid = userGrid;
            TargetGrid = targetGrid;
            StartTime = startTime;
            Delay = delay;
            BreakOnUserMove = breakOnUserMove;
            BreakOnTargetMove = breakOnTargetMove;
            TargetUid = targetUid;
        }
    }
}

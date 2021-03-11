#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class SuspicionMessages
    {
        [Serializable, NetSerializable]
        public sealed class SetSuspicionEndTimerMessage : EntityEventArgs
        {
            public TimeSpan? EndTime;
        }
    }
}

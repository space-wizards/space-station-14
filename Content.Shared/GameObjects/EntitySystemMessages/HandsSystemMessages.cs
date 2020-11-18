#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.EntitySystemMessages
{
    public static class HandsSystemMessages
    {
        [Serializable, NetSerializable]
        public sealed class ChangeHandMessage : EntitySystemMessage
        {
            public ChangeHandMessage(string? index)
            {
                Index = index;
            }

            public string? Index { get; }
        }
    }
}

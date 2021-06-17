#nullable enable
using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameWindow
{
    [Serializable, NetSerializable]
    public sealed class RequestWindowAttentionEvent : EntityEventArgs
    {
    }
}

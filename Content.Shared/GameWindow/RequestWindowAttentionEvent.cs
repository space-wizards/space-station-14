using Robust.Shared.Serialization;

namespace Content.Shared.GameWindow
{
    [Serializable, NetSerializable]
    public sealed partial class RequestWindowAttentionEvent : EntityEventArgs
    {
    }
}


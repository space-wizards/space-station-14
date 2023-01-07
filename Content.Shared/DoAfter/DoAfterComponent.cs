using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
public sealed class DoAfterComponent : Component
{
    public readonly Dictionary<byte, DoAfter> DoAfters = new();
    public readonly Dictionary<byte, DoAfter> CancelledDoAfters = new();

    // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
    // we'll just send them the index. Doesn't matter if it wraps around.
    public byte RunningIndex;
}

[Serializable, NetSerializable]
public sealed class DoAfterComponentState : ComponentState
{
    public Dictionary<byte, DoAfter> DoAfters;

    public DoAfterComponentState(Dictionary<byte, DoAfter> doAfters)
    {
        DoAfters = doAfters;
    }
}

/// <summary>
/// Use this event to raise your DoAfter events now.
/// Check for cancelled, and if it is, then null the token there.
/// </summary>
[Serializable, NetSerializable]
public sealed class DoAfterEvent : HandledEntityEventArgs
{
    public bool Cancelled;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(bool cancelled, DoAfterEventArgs args)
    {
        Cancelled = cancelled;
        Args = args;
    }
}

[Serializable, NetSerializable]
public sealed class CancelledDoAfterMessage : EntityEventArgs
{
    public EntityUid Uid;
    public byte ID;

    public CancelledDoAfterMessage(EntityUid uid, byte id)
    {
        Uid = uid;
        ID = id;
    }
}

[Serializable, NetSerializable]
public enum DoAfterStatus : byte
{
    Running,
    Cancelled,
    Finished,
}

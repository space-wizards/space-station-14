using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter;

[RegisterComponent, NetworkedComponent]
public sealed class DoAfterComponent : Component
{
    [DataField("doAfters")]
    public readonly Dictionary<byte, DoAfter> DoAfters = new();

    [DataField("cancelledDoAfters")]
    public readonly Dictionary<byte, DoAfter> CancelledDoAfters = new();

    // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
    // we'll just send them the index. Doesn't matter if it wraps around.
    [DataField("runningIndex")]
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
/// TODO: Add a networked DoAfterEvent to pass in AdditionalData for the future
[Serializable, NetSerializable]
public sealed class DoAfterEvent : HandledEntityEventArgs
{
    public bool Cancelled;
    public byte Id;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(bool cancelled, DoAfterEventArgs args, byte id)
    {
        Cancelled = cancelled;
        Args = args;
        Id = id;
    }
}

/// <summary>
/// Use this event to raise your DoAfter events now.
/// Check for cancelled, and if it is, then null the token there.
/// Can't be serialized
/// </summary>
/// TODO: Net/Serilization isn't supported so this needs to be networked somehow
public sealed class DoAfterEvent<T> : HandledEntityEventArgs
{
    public T AdditionalData;
    public bool Cancelled;
    public byte Id;
    public readonly DoAfterEventArgs Args;

    public DoAfterEvent(T additionalData, bool cancelled, DoAfterEventArgs args, byte id)
    {
        AdditionalData = additionalData;
        Cancelled = cancelled;
        Args = args;
        Id = id;
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

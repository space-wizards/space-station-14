
using Robust.Shared.Serialization;

namespace Content.Shared.Tips;

[Serializable, NetSerializable]
public sealed class TippyEvent : EntityEventArgs
{
    public TippyEvent(string msg)
    {
        Msg = msg;
    }

    public string Msg;
    public string? Proto;

    // TODO: Why are these defaults even here, have the caller specify. This get overriden only most of the time.
    public float SpeakTime = 5;
    public float SlideTime = 3;
    public float WaddleInterval = 0.5f;
}

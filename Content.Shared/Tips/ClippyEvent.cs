
using Robust.Shared.Serialization;

namespace Content.Shared.Tips;

[Serializable, NetSerializable]
public sealed class ClippyEvent : EntityEventArgs
{
    public ClippyEvent(string msg)
    {
        Msg = msg;
    }

    public string Msg;
    public string? Proto;
    public float SpeakTime = 5;
    public float SlideTime = 3;
    public float WaddleInterval = 0.5f;
}

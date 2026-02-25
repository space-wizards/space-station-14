
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Tips;

/// <summary>
/// Networked event that makes a client show a message on their screen using tippy or another protoype.
/// </summary>
[Serializable, NetSerializable]
public sealed class TippyEvent(string msg, EntProtoId? proto, float speakTime, float slideTime, float waddleInterval) : EntityEventArgs
{
    /// <summary>
    /// The text to show in the speech bubble.
    /// </summary>
    public string Msg = msg;

    /// <summary>
    /// The entity to show. Defaults to tippy.
    /// </summary>
    public EntProtoId? Proto = proto;

    /// <summary>
    /// The time the speech bubble is shown, in seconds.
    /// </summary>
    public float SpeakTime = speakTime;

    /// <summary>
    /// The time the entity takes to walk onto the screen, in seconds.
    /// </summary>
    public float SlideTime = slideTime;

    /// <summary>
    /// The time between waddle animation steps, in seconds.
    /// </summary>
    public float WaddleInterval = waddleInterval;
}

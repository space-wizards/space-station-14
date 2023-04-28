using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVoiceMessage : BoundUserInterfaceMessage
{
    public string Voice { get; }

    public VoiceMaskChangeVoiceMessage(string voice)
    {
        Voice = voice;
    }
}

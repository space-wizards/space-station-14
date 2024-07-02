using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceRecorder;

[Serializable, NetSerializable]
public sealed partial class VoiceRecorderCleaningDoAfterEvent : SimpleDoAfterEvent
{
}

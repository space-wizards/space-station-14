using Robust.Shared.Serialization;

namespace Content.Shared.TapeRecorder
{
    [Serializable, NetSerializable]
    public enum TapeRecorderVisuals : byte
    {
        Status
    }
    [Serializable, NetSerializable]
    public enum TapeRecorderState : byte
    {
        Play,
        Record,
        Rewind,
        Idle
    }

}

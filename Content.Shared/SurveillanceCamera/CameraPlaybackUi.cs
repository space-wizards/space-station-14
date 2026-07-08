using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

[Serializable, NetSerializable]
public enum CameraPlaybackConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class CameraPlaybackConsoleState(
    List<CameraSightingRecord> records,
    TimeSpan oldestTime,
    TimeSpan newestTime,
    TimeSpan target)
    : BoundUserInterfaceState
{
    public List<CameraSightingRecord> Records = records;
    public TimeSpan OldestTime = oldestTime;
    public TimeSpan NewestTime = newestTime;
    public TimeSpan Target = target;
}

[Serializable, NetSerializable]
public sealed class CameraPlaybackTargetRequestMessage(TimeSpan target) : BoundUserInterfaceMessage
{
    public TimeSpan Target = target;
}

public static class CameraPlaybackConstants
{
    public static readonly TimeSpan SliceWindow = TimeSpan.FromSeconds(10);
}

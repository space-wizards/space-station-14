using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SurveillanceCamera;

[Serializable, NetSerializable]
public sealed class CameraSightingRecord
{
    public CameraSightingRecord(TimeSpan time, string cameraName, NetCoordinates position, string perceivedName)
    {
        Time = time;
        CameraName = cameraName;
        Position = position;
        PerceivedName = perceivedName;
    }

    public TimeSpan Time;
    public string CameraName;
    public string CameraAddress = string.Empty;
    public NetCoordinates Position;
    public string PerceivedName;
}

public static class CameraSightingConstants
{
    public const string NET_COMMAND_STRING = "surveillance_camera_sighting";
    public const string NET_SIGHTINGS = "sightings";
}

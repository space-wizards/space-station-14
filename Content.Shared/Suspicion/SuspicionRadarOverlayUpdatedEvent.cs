using System.Numerics;
using Robust.Shared.Serialization;

namespace Content.Shared.Suspicion;

/// <summary>
/// Holds the information for the traitor and detective radar.
/// </summary>
[Serializable, NetSerializable]
public sealed class SuspicionRadarOverlayUpdatedEvent : EntityEventArgs
{
    public readonly RadarInfo[] RadarInfos;

    public SuspicionRadarOverlayUpdatedEvent(RadarInfo[] radarInfos)
    {
        RadarInfos = radarInfos;
    }
}

[Serializable, NetSerializable]
public sealed class RadarInfo
{
    /// <summary>
    /// The color of the radar blip.
    /// </summary>
    public readonly Color Color;

    public readonly Vector2 Position;

    public RadarInfo(Color color, Vector2 position)
    {
        Color = color;
        Position = position;
    }
}


[Serializable, NetSerializable]
public sealed class OnSuspicionRadarOverlayToggledEvent : EntityEventArgs
{
    public readonly bool IsEnabled;

    public OnSuspicionRadarOverlayToggledEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

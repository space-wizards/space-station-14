using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
public sealed class NavInterfaceState(
    float maxRange,
    NetCoordinates? coordinates,
    Angle? angle,
    Dictionary<NetEntity, List<DockingPortState>> docks)
{
    public float MaxRange = maxRange;

    /// <summary>
    /// The relevant coordinates to base the radar around.
    /// </summary>
    public NetCoordinates? Coordinates = coordinates;

    /// <summary>
    /// The relevant rotation to rotate the angle around.
    /// </summary>
    public Angle? Angle = angle;

    public Dictionary<NetEntity, List<DockingPortState>> Docks = docks;

    public bool RotateWithEntity = true;
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key,
}

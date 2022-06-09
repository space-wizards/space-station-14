using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
[Virtual]
public class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly float Range;
    public readonly EntityUid? Entity;
    public readonly List<(EntityCoordinates Coordinates, Angle Angle)> Docks;

    public RadarConsoleBoundInterfaceState(
        float range,
        EntityUid? entity,
        List<(EntityCoordinates Coordinates, Angle Angle)> docks)
    {
        Range = range;
        Entity = entity;
        Docks = docks;
    }
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}

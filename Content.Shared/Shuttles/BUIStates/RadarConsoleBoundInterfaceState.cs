using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.BUIStates;

[Serializable, NetSerializable]
[Virtual]
public class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public readonly float Range;
    public readonly EntityUid? Entity;

    public RadarConsoleBoundInterfaceState(float range, EntityUid ?entity)
    {
        Range = range;
        Entity = entity;
    }
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}

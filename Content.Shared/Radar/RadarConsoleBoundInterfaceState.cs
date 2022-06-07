using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Radar;

[Serializable, NetSerializable]
public sealed class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float Range;
    public EntityUid? Entity;

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

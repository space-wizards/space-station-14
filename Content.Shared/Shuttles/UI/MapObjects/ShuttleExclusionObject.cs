using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.UI.MapObjects;

[Serializable, NetSerializable]
public record struct ShuttleExclusionObject(NetCoordinates Coordinates, string Name, float Range) : IMapObject;

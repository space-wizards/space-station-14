using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Waypointer.Events;

/// <summary>
/// A message networked to a specific client with waypointers containing the data of tracked entity locations.
/// </summary>
/// <param name="coordinates">The locational data of the tracked entities.</param>
[Serializable, NetSerializable]
public sealed class WaypointerUpdatedMessage(Dictionary<ProtoId<WaypointerPrototype>, List<(NetEntity, Vector2)>> coordinates) : EntityEventArgs
{
    public Dictionary<ProtoId<WaypointerPrototype>, List<(NetEntity, Vector2)>> Coordinates = coordinates;
}

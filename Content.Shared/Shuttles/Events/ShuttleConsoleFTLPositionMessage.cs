using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to travel somewhere.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ShuttleConsoleFTLPositionMessage : BoundUserInterfaceMessage
{
    public MapCoordinates Coordinates;
    public Angle Angle;
}


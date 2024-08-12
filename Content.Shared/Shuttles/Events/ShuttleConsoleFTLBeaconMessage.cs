using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on a client when it wishes to FTL to a beacon.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleConsoleFTLBeaconMessage : BoundUserInterfaceMessage
{
    public NetEntity Beacon;
    public Angle Angle;
}

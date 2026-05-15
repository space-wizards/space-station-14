using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// For debugging the expected emergency shuttle position.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmergencyShuttlePositionMessage : EntityEventArgs
{
    public NetEntity? StationUid;
    public Box2? Position;
}

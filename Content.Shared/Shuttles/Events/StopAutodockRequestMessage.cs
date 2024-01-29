using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on a client when it is no longer viewing a dock.
/// </summary>
[Serializable, NetSerializable]
public sealed class StopAutodockRequestMessage : BoundUserInterfaceMessage
{
    public NetEntity DockEntity;
}

using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to not have 2 docking ports docked.
/// </summary>
[Serializable, NetSerializable]
public sealed class UndockRequestMessage : BoundUserInterfaceMessage
{
    public NetEntity DockEntity;
}

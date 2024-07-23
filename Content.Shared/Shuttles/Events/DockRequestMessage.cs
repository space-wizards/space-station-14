using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on the client when it's viewing a particular docking port to try and dock it.
/// </summary>
[Serializable, NetSerializable]
public sealed class DockRequestMessage : BoundUserInterfaceMessage
{
    public NetEntity DockEntity;

    public NetEntity TargetDockEntity;
}

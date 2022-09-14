using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on the client when it wishes to travel somewhere.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleConsoleDestinationMessage : BoundUserInterfaceMessage
{
    public EntityUid Destination;
}

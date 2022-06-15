using Content.Shared.Shuttles.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised by the client to request the server change a particular shuttle's mode.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShuttleModeRequestMessage : BoundUserInterfaceMessage
{
    public ShuttleMode Mode;
}

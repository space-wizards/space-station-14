using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Events;

/// <summary>
/// Raised on a client IFF Console when it wishes to hide IFF.
/// </summary>
[Serializable, NetSerializable]
public sealed class IFFHideIFFMessage : BoundUserInterfaceMessage
{

}

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
/// Raised on a cargo console requesting the cargo shuttle.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoCallShuttleMessage : BoundUserInterfaceMessage
{

}

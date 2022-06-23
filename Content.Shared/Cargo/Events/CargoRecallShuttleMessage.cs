using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
/// Raised on a client request cargo shuttle recall
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoRecallShuttleMessage : BoundUserInterfaceMessage
{

}

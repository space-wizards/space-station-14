using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
/// Raised on a client request to refresh the pallet console
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoPalletAppraiseMessage : BoundUserInterfaceMessage
{

}

using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Set order in database as approved.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleApproveOrderMessage : BoundUserInterfaceMessage
{
    public int OrderIndex;

    public CargoConsoleApproveOrderMessage(int orderIndex)
    {
        OrderIndex = orderIndex;
    }
}

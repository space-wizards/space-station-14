using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Remove order from database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleRemoveOrderMessage : BoundUserInterfaceMessage
{
    public int OrderId;

    public CargoConsoleRemoveOrderMessage(int orderId)
    {
        OrderId = orderId;
    }
}

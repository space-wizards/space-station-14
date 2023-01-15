using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Remove order from database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleRemoveOrderMessage : BoundUserInterfaceMessage
{
    public int OrderIndex;

    public CargoConsoleRemoveOrderMessage(int orderIndex)
    {
        OrderIndex = orderIndex;
    }
}

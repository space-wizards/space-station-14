using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.Events;

/// <summary>
///     Add order to database.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoConsoleAddOrderMessage : BoundUserInterfaceMessage
{
    public string Requester;
    public string Reason;
    public List<CargoOrderItemData> Basket;

    public CargoConsoleAddOrderMessage(string requester, string reason, List<CargoOrderItemData> basket)
    {
        Requester = requester;
        Reason = reason;
        Basket = basket;
    }
}

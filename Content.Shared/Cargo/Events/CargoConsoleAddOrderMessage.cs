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
    public string ProductId;
    public int Amount;

    public CargoConsoleAddOrderMessage(string requester, string reason, string productId, int amount)
    {
        Requester = requester;
        Reason = reason;
        ProductId = productId;
        Amount = amount;
    }
}
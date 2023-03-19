using Robust.Shared.Serialization;

namespace Content.Shared.Shipyard.Events;

/// <summary>
///     Sell a Vessel from the console. The button holds no info and is doing a validation check for a deed client side, but we will still check on the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class ShipyardConsoleSellMessage : BoundUserInterfaceMessage
{
    public ShipyardConsoleSellMessage()
    {
    }
}

using Content.Shared.Cargo;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class CargoAppUiState : BoundUserInterfaceState
{
    public CargoOrderData[] Orders;
    public CargoOrderData[] DeliveredOrders;

    public CargoAppUiState(CargoOrderData[] orders, CargoOrderData[] deliveredOrders)
    {
        Orders = orders;
        DeliveredOrders = deliveredOrders;
    }
}

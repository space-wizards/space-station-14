using Content.Shared.Cargo;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class OrderLogUiState : BoundUserInterfaceState
{
    public CargoOrderData[] Orders;

    public OrderLogUiState(CargoOrderData[] orders)
    {
        Orders = orders;
    }
}

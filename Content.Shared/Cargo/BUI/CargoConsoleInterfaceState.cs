using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public NetEntity Station;
    public List<CargoOrderData> Orders;

    public CargoConsoleInterfaceState(string name, int count, int capacity, NetEntity station, List<CargoOrderData> orders)
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        Station = station;
        Orders = orders;
    }
}

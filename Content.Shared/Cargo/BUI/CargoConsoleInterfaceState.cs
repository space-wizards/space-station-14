using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public int Balance;
    public bool IsSyndicate;
    public List<CargoOrderData> Orders;

    public CargoConsoleInterfaceState(string name, int count, int capacity, int balance, bool isSyndicate, List<CargoOrderData> orders)
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        Balance = balance;
        IsSyndicate = isSyndicate;
        Orders = orders;
    }
}

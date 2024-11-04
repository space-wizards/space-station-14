using Robust.Shared.Serialization;
using Content.Shared.CrewManifest;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public int Balance;
    public List<CargoOrderData> Orders;
    public CrewManifestEntries? CrewManifest;

    // Harmony change -- crewManifest added for cargo orders QoL (Crew list)
    public CargoConsoleInterfaceState(string name, int count, int capacity, int balance, List<CargoOrderData> orders, CrewManifestEntries? crewManifest)
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        Balance = balance;
        Orders = orders;
        CrewManifest = crewManifest;
    }
}

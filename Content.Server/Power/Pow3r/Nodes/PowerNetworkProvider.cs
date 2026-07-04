using Content.Server.Collections;
using Content.Shared.Collections;
using Content.Shared.Power.Pow3r;

namespace Content.Server.Power.Pow3r.Nodes;

public sealed class PowerNetworkProvider : IPowerNetwork
{
    public PowerNetworkProvider(GenIdStorage<PowerNetwork> storage)
    {
        Storage = storage;
    }

    public NodeId Id { get; set; }

    public GenIdStorage<PowerNetwork> Storage;

    public HashSet<EntityUid> Chargers
    {
        get => Storage[Id].Chargers;
        set => Storage[Id].Chargers = value;
    }

    public HashSet<EntityUid> Dischargers
    {
        get => Storage[Id].Dischargers;
        set => Storage[Id].Dischargers = value;
    }

    public HashSet<EntityUid> Consumers
    {
        get => Storage[Id].Consumers;
        set => Storage[Id].Consumers = value;
    }

    public HashSet<EntityUid> Suppliers
    {
        get => Storage[Id].Suppliers;
        set => Storage[Id].Suppliers = value;
    }

    public float LastCombinedLoad
    {
        get => Storage[Id].LastCombinedLoad;
        set => Storage[Id].LastCombinedLoad = value;
    }

    public float LastCombinedSupply
    {
        get => Storage[Id].LastCombinedSupply;
        set => Storage[Id].LastCombinedSupply = value;
    }

    public float LastCombinedMaxSupply
    {
        get => Storage[Id].LastCombinedMaxSupply;
        set => Storage[Id].LastCombinedMaxSupply = value;
    }

    public int Height
    {
        get => Storage[Id].Height;
        set => Storage[Id].Height = value;
    }

    public List<NodeId> Supplies
    {
        get => Storage[Id].Supplies;
        set => Storage[Id].Supplies  = value;
    }

    public List<NodeId> Loads
    {
        get => Storage[Id].Loads;
        set => Storage[Id].Loads  = value;
    }

    public List<NodeId> BatteryLoads
    {
        get => Storage[Id].BatteryLoads;
        set => Storage[Id].BatteryLoads  = value;
    }

    public List<NodeId> BatterySupplies
    {
        get => Storage[Id].BatterySupplies;
        set => Storage[Id].BatterySupplies  = value;
    }
}

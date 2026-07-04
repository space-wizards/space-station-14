using Content.Server.Collections;
using Content.Shared.Collections;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r.Nodes;

public sealed class PowerSupplyProvider : IPowerSupply
{
    public PowerSupplyProvider(GenIdStorage<PowerSupply> storage)
    {
        Storage = storage;
    }

    public NodeId Id { get; set; }

    public GenIdStorage<PowerSupply> Storage;

    public NodeId LinkedNetwork
    {
        get => Storage[Id].LinkedNetwork;
        set => Storage[Id].LinkedNetwork = value;
    }

    public bool Enabled
    {
        get => Storage[Id].Enabled;
        set => Storage[Id].Enabled = value;
    }

    public bool Paused
    {
        get => Storage[Id].Paused;
        set => Storage[Id].Paused = value;
    }

    public float MaxSupply
    {
        get => Storage[Id].MaxSupply;
        set => Storage[Id].MaxSupply = value;
    }

    public float SupplyRampRate
    {
        get => Storage[Id].SupplyRampRate;
        set => Storage[Id].SupplyRampRate = value;
    }

    public float SupplyRampTolerance
    {
        get => Storage[Id].SupplyRampTolerance;
        set => Storage[Id].SupplyRampTolerance = value;
    }

    public float CurrentSupply
    {
        get => Storage[Id].CurrentSupply;
        set => Storage[Id].CurrentSupply = value;
    }

    public float SupplyRampTarget
    {
        get => Storage[Id].SupplyRampTarget;
        set => Storage[Id].SupplyRampTarget = value;
    }

    public float SupplyRampPosition
    {
        get => Storage[Id].SupplyRampPosition;
        set => Storage[Id].SupplyRampPosition = value;
    }

    public float AvailableSupply
    {
        get => Storage[Id].AvailableSupply;
        set => Storage[Id].AvailableSupply = value;
    }
}

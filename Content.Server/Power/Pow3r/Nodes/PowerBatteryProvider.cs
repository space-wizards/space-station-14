using Content.Server.Collections;
using Content.Shared.Collections;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r.Nodes;

public sealed class PowerBatteryProvider : IPowerBattery
{
    public PowerBatteryProvider(GenIdStorage<PowerBattery> storage)
    {
        Storage = storage;
    }

    public NodeId Id { get; set; }

    public GenIdStorage<PowerBattery> Storage;

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

    public NodeId LinkedNetworkCharging
    {
        get => Storage[Id].LinkedNetworkCharging;
        set => Storage[Id].LinkedNetworkCharging = value;
    }

    public NodeId LinkedNetworkDischarging
    {
        get => Storage[Id].LinkedNetworkDischarging;
        set => Storage[Id].LinkedNetworkDischarging = value;
    }

    public bool CanDischarge
    {
        get => Storage[Id].CanDischarge;
        set => Storage[Id].CanDischarge = value;
    }

    public bool CanCharge
    {
        get => Storage[Id].CanCharge;
        set => Storage[Id].CanCharge = value;
    }

    public float Capacity
    {
        get => Storage[Id].Capacity;
        set => Storage[Id].Capacity = value;
    }

    public float MaxChargeRate
    {
        get => Storage[Id].MaxChargeRate;
        set => Storage[Id].MaxChargeRate = value;
    }

    public float MaxThroughput
    {
        get => Storage[Id].MaxThroughput;
        set => Storage[Id].MaxThroughput = value;
    }

    public float MaxSupply
    {
        get => Storage[Id].MaxSupply;
        set => Storage[Id].MaxSupply = value;
    }

    public float SupplyRampTolerance
    {
        get => Storage[Id].SupplyRampTolerance;
        set => Storage[Id].SupplyRampTolerance = value;
    }

    public float SupplyRampRate
    {
        get => Storage[Id].SupplyRampRate;
        set => Storage[Id].SupplyRampRate = value;
    }

    public float Efficiency
    {
        get => Storage[Id].Efficiency;
        set => Storage[Id].Efficiency = value;
    }

    // == Runtime parameters ==

    public float SupplyRampPosition
    {
        get => Storage[Id].SupplyRampPosition;
        set => Storage[Id].SupplyRampPosition = value;
    }

    public float CurrentSupply
    {
        get => Storage[Id].CurrentSupply;
        set => Storage[Id].CurrentSupply = value;
    }

    public float CurrentStorage
    {
        get => Storage[Id].CurrentStorage;
        set => Storage[Id].CurrentStorage = value;
    }

    public float CurrentReceiving
    {
        get => Storage[Id].CurrentReceiving;
        set => Storage[Id].CurrentReceiving = value;
    }

    public float LoadingNetworkDemand
    {
        get => Storage[Id].LoadingNetworkDemand;
        set => Storage[Id].LoadingNetworkDemand = value;
    }

    public bool SupplyingMarked
    {
        get => Storage[Id].SupplyingMarked;
        set => Storage[Id].SupplyingMarked = value;
    }

    public bool LoadingMarked
    {
        get => Storage[Id].LoadingMarked;
        set => Storage[Id].LoadingMarked = value;
    }

    public float AvailableSupply
    {
        get => Storage[Id].AvailableSupply;
        set => Storage[Id].AvailableSupply = value;
    }

    public float DesiredPower
    {
        get => Storage[Id].DesiredPower;
        set => Storage[Id].DesiredPower = value;
    }

    public float SupplyRampTarget
    {
        get => Storage[Id].SupplyRampTarget;
        set => Storage[Id].SupplyRampTarget = value;
    }

    public float MaxEffectiveSupply
    {
        get => Storage[Id].MaxEffectiveSupply;
        set => Storage[Id].MaxEffectiveSupply = value;
    }
}

using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public struct PowerBattery : IPowerBattery
{
    public PowerBattery()
    {
    }

    public NodeId Id { get; set; } = default;
    public NodeId LinkedNetworkCharging { get; set; } = default;
    public NodeId LinkedNetworkDischarging { get; set; } = default;
    public bool Enabled { get; set; } = true;
    public bool Paused { get; set; } = false;
    public bool CanDischarge { get; set; } = true;
    public bool CanCharge { get; set; } = true;
    public float Capacity { get; set; } = 0f;
    public float MaxChargeRate { get; set; } = 0f;
    public float MaxThroughput { get; set; } = 0f;
    public float MaxSupply { get; set; } = 0f;
    public float SupplyRampTolerance { get; set; } = 5000f;
    public float SupplyRampRate { get; set; } = 5000f;
    public float Efficiency { get; set; } = 1f;

    public float SupplyRampPosition { get; set; } = 0f;
    public float CurrentSupply { get; set; } = 0f;
    public float CurrentStorage { get; set; } = 0f;
    public float CurrentReceiving { get; set; } = 0f;
    public float LoadingNetworkDemand { get; set; } = 0f;
    public bool SupplyingMarked { get; set; } = false;
    public bool LoadingMarked { get; set; } = false;
    public float AvailableSupply { get; set; } = 0f;
    public float DesiredPower { get; set; } = 0f;
    public float SupplyRampTarget { get; set; } = 0f;
    public float MaxEffectiveSupply { get; set; } = 0f;
}

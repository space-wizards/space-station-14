namespace Content.Shared.Power.Pow3r.Nodes;

public struct PowerBattery : IPowerBattery
{
    public bool Enabled { get; set; }
    public bool Paused { get; set; }
    public bool CanDischarge { get; set; }
    public bool CanCharge { get; set; }
    public float Capacity { get; set; }
    public float MaxChargeRate { get; set; }
    public float MaxThroughput { get; set; }
    public float MaxSupply { get; set; }
    public float SupplyRampTolerance { get; set; }
    public float SupplyRampRate { get; set; }
    public float Efficiency { get; set; }
    public float SupplyRampPosition { get; set; }
    public float CurrentSupply { get; set; }
    public float CurrentStorage { get; set; }
    public float CurrentReceiving { get; set; }
    public float LoadingNetworkDemand { get; set; }
    public bool SupplyingMarked { get; set; }
    public bool LoadingMarked { get; set; }
    public float AvailableSupply { get; set; }
    public float DesiredPower { get; set; }
    public float SupplyRampTarget { get; set; }
    public float MaxEffectiveSupply { get; set; }
}

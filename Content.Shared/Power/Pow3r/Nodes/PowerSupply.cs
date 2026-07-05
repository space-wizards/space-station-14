using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public struct PowerSupply : IPowerSupply
{
    public PowerSupply()
    {
    }

    public NodeId Id { get; set; } = default;
    public NodeId LinkedNetwork { get; set; } = default;
    public bool Enabled { get; set; } = true;
    public bool Paused { get; set; } = false;
    public float MaxSupply { get; set; } = 0f;
    public float SupplyRampRate { get; set; } = 5000f;
    public float SupplyRampTolerance { get; set; } = 5000f;

    public float CurrentSupply { get; set; } = 0f;
    public float SupplyRampTarget { get; set; } = 0f;
    public float SupplyRampPosition { get; set; } = 0f;
    public float AvailableSupply { get; set; } = 0f;
}

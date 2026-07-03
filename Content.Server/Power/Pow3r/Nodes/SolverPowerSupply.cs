using Content.Server.Collections;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r.Nodes;

public struct SolverPowerSupply : IPowerSupply
{
    public NodeId LinkedNetwork { get; set; }
    public bool Enabled { get; set; }
    public bool Paused { get; set; }
    public float MaxSupply { get; set; }
    public float SupplyRampRate { get; set; }
    public float SupplyRampTolerance { get; set; }
    public float CurrentSupply { get; set; }
    public float SupplyRampTarget { get; set; }
    public float SupplyRampPosition { get; set; }
    public float AvailableSupply { get; set; }
}

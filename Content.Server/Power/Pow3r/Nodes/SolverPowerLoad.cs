using Content.Server.Collections;
using Content.Shared.Power.Pow3r.Nodes;

namespace Content.Server.Power.Pow3r.Nodes;

public struct SolverPowerLoad : IPowerLoad
{
    public NodeId Id { get; set; }
    public NodeId LinkedNetwork { get; set; }
    public bool Enabled { get; set; }
    public bool Paused { get; set; }
    public float DesiredPower { get; set; }
    public float ReceivingPower { get; set; }
}

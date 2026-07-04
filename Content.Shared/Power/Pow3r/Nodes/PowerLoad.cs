using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public struct PowerLoad : IPowerLoad
{
    public NodeId Id { get; set; }
    public NodeId LinkedNetwork { get; set; }
    public bool Enabled { get; set; }
    public bool Paused { get; set; }
    public float DesiredPower { get; set; }
    public float ReceivingPower { get; set; }
}

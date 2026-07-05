using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public struct PowerLoad : IPowerLoad
{
    public PowerLoad()
    {
    }

    public NodeId Id { get; set; } = default;
    public NodeId LinkedNetwork { get; set; } = default;
    public bool Enabled { get; set; } = true;
    public bool Paused { get; set; } = false;
    public float DesiredPower { get; set; } = 0f;

    public float ReceivingPower { get; set; } = 0f;
}

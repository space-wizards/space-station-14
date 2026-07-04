using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerLoad : IPowerNode
{
    NodeId LinkedNetwork { get; set; }

    float DesiredPower { get; set; }

    float ReceivingPower { get; set; }
}

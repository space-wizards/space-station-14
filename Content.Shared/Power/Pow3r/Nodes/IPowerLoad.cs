using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerLoad : IPowerNode
{
    float DesiredPower { get; set; }

    float ReceivingPower { get; set; }

    NodeId LinkedNetwork { get; set; }
}

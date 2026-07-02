using Content.Shared.Collections;

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerNode
{
    NodeId Id { get; set; }

    bool Enabled { get; set; }

    bool Paused { get; set; }
}

namespace Content.Shared.Power.Pow3r.Nodes;

public interface IPowerNode
{
    bool Enabled { get; set; }

    bool Paused { get; set; }
}

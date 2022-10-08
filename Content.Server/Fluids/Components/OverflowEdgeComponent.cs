using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map;

namespace Content.Server.Fluids.Components;

[RegisterComponent]
[Access(typeof(FluidSpreaderSystem))]
public sealed class OverflowEdgeComponent : Component
{
    [ViewVariables] public Solution OverflownSolution;
    [ViewVariables] public Dictionary<Vector2i, EntityUid> ActiveEdge;

    public OverflowEdgeComponent(Solution overflownSolution)
    {
        OverflownSolution = overflownSolution;
        ActiveEdge = new();
    }

    public OverflowEdgeComponent()
    {
        OverflownSolution = new Solution();
        ActiveEdge = new();
    }
}



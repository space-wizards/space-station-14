using Content.Shared.Nodes;
using Robust.Shared.GameStates;

namespace Content.Client.Nodes.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GraphNodeComponent : Component
{
    [ViewVariables]
    public List<EdgeVisState> Edges = new(4);

    [ViewVariables]
    public EntityUid HostId = EntityUid.Invalid;

    [ViewVariables]
    public string GraphProto = "UNDEFINED";

    [ViewVariables]
    public EntityUid? GraphId = null;
}

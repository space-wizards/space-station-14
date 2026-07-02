using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Nodes;

namespace Content.Shared.NodeContainer.Nodes;

public interface IPipeNode : IRotatableNode, IGasMixtureHolder
{
    /// <summary>
    ///     The directions in which this pipe can connect to other pipes around it.
    /// </summary>
    PipeDirection OriginalPipeDirection { get; set; }

    /// <summary>
    ///     The *current* layer to which the pipe node is assigned.
    /// </summary>
    AtmosPipeLayer CurrentPipeLayer { get; set; }

    /// <summary>
    ///     The *current* pipe directions (accounting for rotation)
    ///     Used to check if this pipe can connect to another pipe in a given direction.
    /// </summary>
    PipeDirection CurrentPipeDirection { get; set; }

    HashSet<PipeNode>? AlwaysReachable { get; set; }

    /// <summary>
    ///     Whether this node can connect to others or not.
    /// </summary>
    /// <remarks>
    ///     Always remake the node group when you change this value.
    /// </remarks>
    bool ConnectionsEnabled { get; set; }

    bool RotationsEnabled { get; set; }
}


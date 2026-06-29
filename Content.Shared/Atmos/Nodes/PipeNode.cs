using Content.Shared.Atmos.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Nodes;
using Robust.Shared.Utility;

namespace Content.Shared.Atmos.Nodes;

/// <summary>
///     Connects with other <see cref="PipeNode"/>s whose <see cref="PipeDirection"/>
///     and <see cref="CurrentPipeLayer"/> correctly correspond.
/// </summary>
[Virtual]
[ImplicitDataDefinitionForInheritors]
public partial class PipeNode : Node, IPipeNode
{
    /// <summary>
    ///     The directions in which this pipe can connect to other pipes around it.
    /// </summary>
    [DataField("pipeDirection")]
    public PipeDirection OriginalPipeDirection { get; set; }

    /// <summary>
    ///     The *current* layer to which the pipe node is assigned.
    /// </summary>
    [DataField("pipeLayer")]
    public AtmosPipeLayer CurrentPipeLayer { get; set; } = AtmosPipeLayer.Primary;

    /// <summary>
    ///     The *current* pipe directions (accounting for rotation)
    ///     Used to check if this pipe can connect to another pipe in a given direction.
    /// </summary>
    public PipeDirection CurrentPipeDirection { get; set; }

    public HashSet<PipeNode>? AlwaysReachable { get; set; } = new();

    /// <summary>
    ///     Whether this node can connect to others or not.
    /// </summary>
    /// <remarks>
    ///     Always remake the node group when you change this value.
    /// </remarks>
    [ViewVariables]
    public bool ConnectionsEnabled { get; set; }

    [DataField]
    public bool RotationsEnabled { get; set; } = true;

    /// <summary>
    ///     The <see cref="PipeNet"/> this pipe is a part of.
    /// </summary>
    [ViewVariables]
    private PipeNet? PipeNet => (PipeNet?) NodeGroup;

    /// <summary>
    ///     The gases in this pipe.
    /// </summary>
    [ViewVariables]
    public GasMixture Air
    {
        get => PipeNet?.Air ?? GasMixture.SpaceGas;
        set
        {
            DebugTools.Assert(PipeNet != null);
            PipeNet!.Air = value;
        }
    }

    [DataField]
    public float Volume { get; set; } = 200f;
}

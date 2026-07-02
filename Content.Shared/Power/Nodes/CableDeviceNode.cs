using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.Systems;

namespace Content.Shared.Power.Nodes;

/// <summary>
///     Type of node that connects to a <see cref="CableNode"/> below it.
/// </summary>
[Virtual]
[ImplicitDataDefinitionForInheritors]
public partial class CableDeviceNode : Node
{
    /// <summary>
    /// If disabled, this cable device will never connect.
    /// </summary>
    /// <remarks>
    /// If you change this,
    /// you must manually call <see cref="NodeGroupSystem.QueueReflood"/> to update the node connections.
    /// </remarks>
    [DataField]
    public bool Enabled = true;
}

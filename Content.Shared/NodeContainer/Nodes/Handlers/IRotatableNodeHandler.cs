using Content.Shared.NodeContainer.Systems;

namespace Content.Shared.NodeContainer.Nodes.Handlers;

public interface IRotatableNodeHandler : INodeHandler
{
    /// <summary>
    ///     Rotates this <see cref="Node"/>. Returns true if the node's connections need to be updated.
    /// </summary>
    bool RotateNode(IRotatableNode node, in MoveEvent ev);
}

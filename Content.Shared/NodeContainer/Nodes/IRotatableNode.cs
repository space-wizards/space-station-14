using Content.Shared.NodeContainer.Nodes.Handlers;

namespace Content.Shared.NodeContainer.Nodes;

/// <summary>
///     A <see cref="Node"/> that implements this will have its <see cref="IRotatableNodeHandler.RotateNode"/> called when its
///     <see cref="NodeContainerComponent"/> is rotated.
/// </summary>
public interface IRotatableNode : INode;

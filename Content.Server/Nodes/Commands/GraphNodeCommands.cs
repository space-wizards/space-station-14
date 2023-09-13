using Content.Server.Administration;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using System.Linq;

namespace Content.Server.Nodes.Commands;

/// <summary>
/// Debugging commands related to manipulating graph nodes.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class GraphNodeCommand : ToolshedCommand
{
    private NodeGraphSystem? _sys = null;


    #region graphnode:fix

    /// <summary>Recalculates the edges for all (unpaused) nodes.</summary>
    [CommandImplementation("fix")]
    public void FixAllNodes()
    {
        var nodes = EntityManager.EntityQueryEnumerator<GraphNodeComponent>();
        while (nodes.MoveNext(out var nodeId, out var _))
        {
            FixSingleNode(nodeId);
        }
    }

    /// <summary>Recalculates the edges for a single node.</summary>
    [CommandImplementation("fix")]
    public EntityUid FixSingleNode([PipedArgument] EntityUid nodeId)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        _sys.QueueEdgeUpdate(nodeId);
        return nodeId;
    }

    /// <summary>Recalculates the edges for multiple nodes.</summary>
    [CommandImplementation("fix")]
    public IEnumerable<EntityUid> FixMultipleNodes([PipedArgument] IEnumerable<EntityUid> nodes)
        => nodes.Select(nodeId => FixSingleNode(nodeId));

    /// <inheritdoc cref="FixSingleNode"/>
    [CommandImplementation("fix")]
    public EntityUid FixSingleNode([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId)
        => FixSingleNode(nodeId.Evaluate(context));

    /// <inheritdoc cref="FixMultipleNodes"/>
    [CommandImplementation("fix")]
    public IEnumerable<EntityUid> FixMultipleNodes([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<IEnumerable<EntityUid>> nodes)
        => nodes.Evaluate(context) is { } uids ? FixMultipleNodes(uids) : Array.Empty<EntityUid>();

    #endregion graphnode:fix
}

using Content.Server.Administration;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using System.Linq;

namespace Content.Server.Nodes.Commands;

/// <summary>
/// Debugging commands related to manipulating node graphs.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class NodeGraphCommand : ToolshedCommand
{
    private NodeGraphSystem? _sys = null;


    #region nodegraph:fix

    /// <summary>Makes all graphs check whether they should split into subgraphs/merge with any other graphs.</summary>
    [CommandImplementation("fix")]
    public void FixAllGraphs()
    {
        var nodes = EntityManager.EntityQueryEnumerator<NodeGraphComponent>();
        while (nodes.MoveNext(out var graphId, out var _))
        {
            FixSingleGraph(graphId);
        }
    }

    /// <summary>Makes a specific graph check whether it should split into subgraphs/merge with any other graphs.</summary>
    [CommandImplementation("fix")]
    public EntityUid FixSingleGraph([PipedArgument] EntityUid graphId)
    {
        if (!TryComp<NodeGraphComponent>(graphId, out var graph))
            return graphId;

        _sys ??= GetSys<NodeGraphSystem>();

        foreach (var nodeId in graph.Nodes)
        {
            if (!TryComp<GraphNodeComponent>(nodeId, out var node))
                continue;

            _sys.MarkSplit((nodeId, node));
            _sys.MarkMerge((nodeId, node));
        }

        return graphId;
    }

    #endregion nodegraph:fix
}

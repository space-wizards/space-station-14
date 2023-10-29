using Content.Server.Administration;
using Content.Server.Nodes.EntitySystems;
using Content.Server.Nodes.Components;
using Content.Shared.Administration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using System.Linq;

namespace Content.Server.Nodes.Commands;

/// <summary>
/// Debugging commands related to the node graph visualization.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class NodeVisCommand : ToolshedCommand
{
    private NodeGraphSystem? _sys = null;


    #region nodevis:show

    /// <summary>Enables node graph debugging visualization for the invoking client.</summary>
    [CommandImplementation("show")]
    public void ShowAllGraphs([CommandInvocationContext] IInvocationContext context)
    {
        if (context.Session is not ICommonSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StartSendingNodeVis(session);
    }

    #endregion nodevis:show

    #region nodevis:hide

    /// <summary>Enables node graph debugging visualization for the invoking client.</summary>
    [CommandImplementation("hide")]
    public void HideAllGraphs([CommandInvocationContext] IInvocationContext context)
    {
        if (context.Session is not ICommonSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StartSendingNodeVis(session);
    }

    #endregion nodevis:hide

    #region nodevis:refresh

    /// <summary>Forces a refresh of the node debugging visualization for all node graphs.</summary>
    [CommandImplementation("refresh")]
    public void RefreshAllGraphs()
    {
        var graphQuery = EntityManager.EntityQueryEnumerator<NodeGraphComponent>();
        while (graphQuery.MoveNext(out var graphId, out var _))
        {
            RefreshGraph(graphId);
        }
    }

    /// <summary>Forces a refresh of the node debugging visualization for a specific node graph.</summary>
    [CommandImplementation("refresh")]
    public EntityUid RefreshGraph([PipedArgument] EntityUid graphId)
    {
        if (!TryComp<NodeGraphComponent>(graphId, out var graph))
            return graphId;

        EntityManager.Dirty(graphId, graph);

        foreach (var nodeId in graph.Nodes)
        {
            EntityManager.Dirty(nodeId, Comp<GraphNodeComponent>(nodeId));
        }

        return graphId;
    }

    #endregion nodevis:refresh
}

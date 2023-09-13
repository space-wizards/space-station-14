using Content.Server.Administration;
using Content.Server.Nodes.Components;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Administration;
using Content.Shared.Nodes;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.Syntax;
using System.Linq;

namespace Content.Server.Nodes.Commands;

/// <summary>
/// Debugging commands related to manipulating node graph edges.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class GraphEdgeCommand : ToolshedCommand
{
    private NodeGraphSystem? _sys = null;

    #region graphedge:exists

    /// <inheritdoc cref="HasSingleEdge"/>
    /// <remarks>Takes piped arguments.</remarks>
    [CommandImplementation("exists")]
    public bool HasSingleEdge([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] EntityUid edgeId)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        return _sys.HasEdge(nodeId.Evaluate(context), edgeId);
    }

    /// <inheritdoc cref="HasMultipleEdges"/>
    /// <remarks>Takes piped arguments.</remarks>
    [CommandImplementation("exists")]
    public IEnumerable<bool> HasMultipleEdges([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] IEnumerable<EntityUid> edges)
        => edges.Select(edgeId => HasSingleEdge(context, nodeId, edgeId));

    #endregion graphedge:exists

    #region graphedge:get

    /// <summary>Returns whether a node graph edge exists between two entities.</summary>
    [CommandImplementation("get")]
    public EdgeFlags? GetSingleEdge([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] EntityUid edgeId)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        return _sys.GetEdgeOrNull(nodeId.Evaluate(context), edgeId);
    }

    /// <summary>Returns whether a node graph edge exists an entity and a set of other entities.</summary>
    [CommandImplementation("get")]
    public IEnumerable<EdgeFlags?> GetMultipleEdges([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] IEnumerable<EntityUid> edges)
        => edges.Select(edgeId => GetSingleEdge(context, nodeId, edgeId));

    #endregion graphedge:get

    #region graphedge:add

    /// <summary>Attempts to add a node graph edge between two entities.</summary>
    [CommandImplementation("add")]
    public bool AddSingleEdge([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] EntityUid edgeId, [CommandArgument] ValueRef<EdgeFlags> edgeFlags)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        return _sys.TryAddEdge(nodeId.Evaluate(context), edgeId, flags: edgeFlags.Evaluate(context));
    }

    /// <summary>Attempts to add a node graph edge between an entity and a set of other entities.</summary>
    [CommandImplementation("add")]
    public IEnumerable<bool> AddMultipleEdges([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] IEnumerable<EntityUid> edges, [CommandArgument] ValueRef<EdgeFlags> edgeFlags)
        => edges.Select(edgeId => AddSingleEdge(context, nodeId, edgeId, edgeFlags));

    #endregion graphedge:add

    #region graphedge:remove

    /// <summary>Attempts to remove a node graph edge between two entities.</summary>
    [CommandImplementation("remove")]
    public bool RemoveSingleEdge([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] EntityUid edgeId)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        return _sys.TryRemoveEdge(nodeId.Evaluate(context), edgeId);
    }

    /// <summary>Attempts to remove a node graph edge between an entity and a set of other entities.</summary>
    [CommandImplementation("remove")]
    public IEnumerable<bool> RemoveMultipleEdges([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] IEnumerable<EntityUid> edges)
        => edges.Select(edgeId => RemoveSingleEdge(context, nodeId, edgeId));

    #endregion graphedge:remove

    #region graphedge:set

    /// <summary>Attempts to set the state of a node graph edge between two entities. Can override manually set edges.</summary>
    [CommandImplementation("set")]
    public bool SetSingleEdge([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] EntityUid edgeId, [CommandArgument] ValueRef<EdgeFlags> edgeFlags)
    {
        _sys ??= GetSys<NodeGraphSystem>();
        return _sys.TrySetEdge(nodeId.Evaluate(context), edgeId, flags: edgeFlags.Evaluate(context));
    }

    /// <summary>Attempts to set the state of a node graph edge between an entity and a set of other entities.</summary>
    [CommandImplementation("set")]
    public IEnumerable<bool> SetMultipleEdges([CommandInvocationContext] IInvocationContext context, [CommandArgument] ValueRef<EntityUid> nodeId, [PipedArgument] IEnumerable<EntityUid> edges, [CommandArgument] ValueRef<EdgeFlags> edgeFlags)
        => edges.Select(edgeId => SetSingleEdge(context, nodeId, edgeId, edgeFlags));

    #endregion graphedge:set
}

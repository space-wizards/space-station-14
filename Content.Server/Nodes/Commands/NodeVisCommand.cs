using Content.Server.Administration;
using Content.Server.Nodes.EntitySystems;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Toolshed;
using System.Linq;

namespace Content.Server.Nodes.Commands;

/// <summary>
/// A debugging command that enables or disables node graph debugging visuals for the invoking client.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class NodeVisCommand : ToolshedCommand
{
    private NodeGraphSystem? _sys = null;


    /// <summary>
    /// Enables sending node debugging info to the invoking client.
    /// </summary>
    [CommandImplementation("show")]
    public void ShowNodeVis([CommandInvocationContext] IInvocationContext context)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StartSendingNodeVis(session);
    }

    /// <summary>
    /// Enables sending node debugging info for specific node graphs to the invoking client.
    /// </summary>
    [CommandImplementation("show")]
    public void ShowNodeVis([CommandInvocationContext] IInvocationContext context, [PipedArgument] IEnumerable<EntityUid> uids)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StartSendingNodeVis(session, uids.ToList());
    }

    /// <summary>
    /// Enables sending node debugging info for specific types of node graphs to the invoking client.
    /// </summary>
    [CommandImplementation("show")]
    public void ShowNodeVis([CommandInvocationContext] IInvocationContext context, [PipedArgument] IEnumerable<string> prototypes)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StartSendingNodeVis(session, prototypes.ToList());
    }

    /// <summary>
    /// Disables sending node debugging info to the invoking client.
    /// </summary>
    [CommandImplementation("hide")]
    public void HideNodeVis([CommandInvocationContext] IInvocationContext context)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StopSendingNodeVis(session);
    }

    /// <summary>
    /// Disables sending node debugging info for specific node graphs to the invoking client.
    /// </summary>
    [CommandImplementation("hide")]
    public void HideNodeVis([CommandInvocationContext] IInvocationContext context, [PipedArgument] IEnumerable<EntityUid> uids)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StopSendingNodeVis(session, uids.ToList());
    }

    /// <summary>
    /// Disables sending node debugging info for specific types of node graphs to the invoking client.
    /// </summary>
    [CommandImplementation("hide")]
    public void HideNodeVis([CommandInvocationContext] IInvocationContext context, [PipedArgument] IEnumerable<string> prototypes)
    {
        if (context.Session is not IPlayerSession session)
            return;

        _sys ??= GetSys<NodeGraphSystem>();
        _sys.StopSendingNodeVis(session, prototypes.ToList());
    }
}

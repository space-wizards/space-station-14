using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.FeedbackSystem;

/// <summary>
/// Adds, removes, and displays feedback for specified sessions.
/// </summary>
[ToolshedCommand]
[AdminCommand(AdminFlags.Debug)]
public sealed class FeedbackCommand : ToolshedCommand
{
    [Dependency] private readonly ISharedFeedbackManager _feedback = null!;

    [CommandImplementation("show")]
    public void ExecuteShow([CommandArgument] ICommonSession session)
    {
        _feedback.OpenForSession(session);
    }

    [CommandImplementation("show")]
    public void ExecuteShow([PipedArgument] IEnumerable<ICommonSession> sessions)
    {
        foreach (var session in sessions)
        {
            _feedback.OpenForSession(session);
        }
    }

    [CommandImplementation("add")]
    public void ExecuteAdd([CommandArgument] ICommonSession session, ProtoId<FeedbackPopupPrototype> protoId)
    {
        _feedback.SendToSession(session, [protoId]);
    }

    [CommandImplementation("add")]
    public void ExecuteAdd([PipedArgument] IEnumerable<ICommonSession> sessions, ProtoId<FeedbackPopupPrototype> protoId)
    {
        foreach (var session in sessions)
        {
            _feedback.SendToSession(session, [protoId]);
        }
    }

    [CommandImplementation("remove")]
    public void ExecuteRemove([CommandArgument] ICommonSession session, ProtoId<FeedbackPopupPrototype> protoId)
    {
        _feedback.SendToSession(session, [protoId], true);
    }

    [CommandImplementation("remove")]
    public void ExecuteRemove([PipedArgument] IEnumerable<ICommonSession> sessions, ProtoId<FeedbackPopupPrototype> protoId)
    {
        foreach (var session in sessions)
        {
            _feedback.SendToSession(session, [protoId], true);
        }
    }
}

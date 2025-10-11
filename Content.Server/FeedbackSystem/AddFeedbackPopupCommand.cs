using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.FeedbackSystem;

[ToolshedCommand]
[AdminCommand(AdminFlags.Server)]
public sealed class AddFeedbackPopupCommand : ToolshedCommand
{
    [Dependency] private readonly SharedFeedbackManager _feedback = null!;

    [CommandImplementation]
    public void Execute([CommandArgument] ICommonSession session, ProtoId<FeedbackPopupPrototype> protoId)
    {
        _feedback.SendToSession(session, [protoId]);
    }

    [CommandImplementation]
    public void Execute([PipedArgument] IEnumerable<ICommonSession> sessions, ProtoId<FeedbackPopupPrototype> protoId)
    {
        foreach (var session in sessions)
        {
            _feedback.SendToSession(session, [protoId]);
        }
    }
}

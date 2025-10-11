using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Player;
using Robust.Shared.Toolshed;

namespace Content.Server.FeedbackSystem;

/// <summary>
/// Show the feedback popups for your own client, if there are any.
/// </summary>
[ToolshedCommand]
[AdminCommand(AdminFlags.Server)]
public sealed class ShowFeedbackPopupCommand : ToolshedCommand
{
    [Dependency] private readonly SharedFeedbackManager _feedback = null!;

    [CommandImplementation]
    public void Execute([CommandArgument] ICommonSession session)
    {
            _feedback.OpenForSession(session);
    }

    [CommandImplementation]
    public void Execute([PipedArgument] IEnumerable<ICommonSession> sessions)
    {
        foreach (var session in sessions)
        {
            _feedback.OpenForSession(session);
        }
    }
}

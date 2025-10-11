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
public sealed class ShowFeedbackPopupCommand : ToolshedCommand
{
    [Dependency] private readonly SharedFeedbackManager _feedback = null!;

    [AnyCommand]
    [CommandImplementation]
    public void Execute(IInvocationContext context)
    {
        if (context.Session == null)
            return;

        Execute([context.Session]);
    }

    [AdminCommand(AdminFlags.Server)]
    [CommandImplementation]
    public void Execute([PipedArgument] IEnumerable<ICommonSession> sessions)
    {
        var feedbackProtypes = _feedback.GetOriginFeedbackPrototypes(true);

        if (feedbackProtypes.Count == 0)
            return;

        foreach (var session in sessions)
        {
            _feedback.SendToSession(session, feedbackProtypes);
        }
    }
}

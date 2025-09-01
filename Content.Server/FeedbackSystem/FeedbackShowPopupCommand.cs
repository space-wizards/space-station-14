using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Console;

namespace Content.Server.FeedbackSystem;

/// <summary>
/// Show the feedback popups for your own client, if there are any.
/// </summary>
[AnyCommand]
public sealed class FeedbackShowPopupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedFeedbackSystem _feedback = default!;

    public override string Command => Loc.GetString("feedbackpopup-show-command-name");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var feedbackProtypes = _feedback.GetOriginFeedbackPrototypes(true);

        if (feedbackProtypes.Count == 0 || shell.Player == null)
            return;

        _feedback.SendPopupsSession(shell.Player, feedbackProtypes);
    }
}

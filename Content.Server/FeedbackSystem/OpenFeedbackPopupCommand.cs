using Content.Shared.Administration;
using Content.Shared.FeedbackSystem;
using Robust.Shared.Toolshed;

namespace Content.Server.FeedbackSystem;

/// <summary>
/// Open the feedback popup window
/// </summary>
[AnyCommand]
[ToolshedCommand]
public sealed class OpenFeedbackPopupCommand : ToolshedCommand
{
    [Dependency] private readonly SharedFeedbackManager _feedback = null!;

    [CommandImplementation]
    public void Execute(IInvocationContext context)
    {
        if (context.Session == null)
            return;

        _feedback.OpenForSession(context.Session);
    }
}

using Content.Client.Stylesheets;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.Replay.UI.Loading;

/// <summary>
/// State used to display an error message if a replay failed to load.
/// </summary>
/// <seealso cref="ReplayLoadingFailedControl"/>
/// <seealso cref="ContentReplayPlaybackManager"/>
public sealed class ReplayLoadingFailed : State
{
    [Dependency] private readonly IStylesheetManager _stylesheetManager = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    private ReplayLoadingFailedControl? _control;

    public void SetData(Exception exception, Action? cancelPressed, Action? retryPressed)
    {
        DebugTools.Assert(_control != null);
        _control.SetData(exception, cancelPressed, retryPressed);
    }

    protected override void Startup()
    {
        _control = new ReplayLoadingFailedControl(_stylesheetManager);
        _userInterface.StateRoot.AddChild(_control);
    }

    protected override void Shutdown()
    {
        _control?.Orphan();
    }
}

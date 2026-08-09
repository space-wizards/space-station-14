using Content.Client.GameTicking.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.RoundEnd;

[UsedImplicitly]
public sealed partial class RoundEndSummaryUIController : UIController,
    IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private IInputManager _input = default!;

    /// <summary>
    /// Raised when the round summary window is opened or closed.
    /// Argument is true when window is open
    /// </summary>
    public event Action<bool>? OnWindowToggled;

    private RoundEndMessageInfo? _lastRoundInfo = null;
    private RoundEndSummaryWindow? _window;

    public void ToggleRoundEndSummaryWindow(ICommonSession? session = null)
    {
        if (_window == null)
        {
            if (_lastRoundInfo != null)
                OpenRoundEndSummaryWindow(_lastRoundInfo);
            return;
        }

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.OpenCenteredRight();
            _window.MoveToFront();
        }

        OnWindowToggled?.Invoke(_window.IsOpen);
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageInfo roundInfo)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == roundInfo.RoundId)
            return;

        _lastRoundInfo = roundInfo;
        _window = new RoundEndSummaryWindow(roundInfo);
        _window.OnClose += () => OnWindowToggled?.Invoke(false);

        _window.OpenCenteredRight();
        _window.MoveToFront();
        OnWindowToggled?.Invoke(true);
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleRoundEndSummaryWindow));
    }

    public void UpdateRoundInfo(RoundEndMessageInfo roundInfo)
    {
        _lastRoundInfo = roundInfo;
    }

    /// <summary>
    /// Returns true if we have the information to open the round summary window
    /// </summary>
    public bool IsSummaryValid()
    {
        return _lastRoundInfo != null;
    }
}

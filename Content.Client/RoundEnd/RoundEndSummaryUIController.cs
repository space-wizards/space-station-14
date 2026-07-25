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

    private RoundEndSummaryWindow? _window;

    public void ToggleScoreboardWindow(ICommonSession? session = null)
    {
        if (_window == null)
            return;

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

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo);
        _window.OnClose += () => OnWindowToggled?.Invoke(false);
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleScoreboardWindow));
    }

    /// <summary>
    /// Returns true if we have the information to open the round summary window
    /// </summary>
    public bool IsSummaryValid()
    {
        return _window != null;
    }

    /// <summary>
    /// Return true if the round summary window is currently open
    /// </summary>
    public bool IsSummaryOpen()
    {
        return _window != null && _window.IsOpen;
    }
}

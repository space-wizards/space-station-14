using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.Scoreboard;

public sealed class ScoreboardUIController : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>, IOnStateExited<LobbyState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IInputManager _input = default!;

    private RoundEndSummaryWindow? _window;

    public void OnStateEntered(LobbyState state)
    {
        HandleStateEntered();
    }

    public void OnStateEntered(GameplayState state)
    {
        HandleStateEntered();
    }

    public void OnStateExited(LobbyState state)
    {
        HandleStateExited();
    }

    public void OnStateExited(GameplayState state)
    {
        HandleStateExited();
    }

    private void HandleStateEntered()
    {
        _input.SetInputCommand(ContentKeyFunctions.OpenScoreboardWindow,
            InputCmdHandler.FromDelegate(_ =>
            {
                if (_window != null)
                    OpenScoreboardWindow(_window);
            }));
    }

    private void HandleStateExited()
    {
        CommandBinds.Unregister<ScoreboardUIController>();
    }

    private void OpenScoreboardWindow(RoundEndSummaryWindow window)
    {
        window.OpenCenteredRight();
        window.MoveToFront();
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);
    }
}

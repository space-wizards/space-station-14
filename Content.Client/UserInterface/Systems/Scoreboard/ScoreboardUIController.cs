using Content.Client.Gameplay;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Client.RoundEnd;
using Content.Shared.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.Scoreboard;

public sealed class ScoreboardUIController : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>, IOnStateExited<LobbyState>, IOnStateExited<GameplayState>
{

    [Dependency] private readonly IInputManager _input = default!;
    [UISystemDependency] private readonly ClientGameTicker _gameTicker = default!;

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
                if (_gameTicker.Window != null)
                    OpenScoreboardWindow(_gameTicker.Window);
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
}

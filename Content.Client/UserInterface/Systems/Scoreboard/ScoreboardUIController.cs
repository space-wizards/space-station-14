using Content.Client.RoundEnd;
using Robust.Client.Input;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby;
using Content.Client.Gameplay;
using Robust.Shared.Log;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.UserInterface.Scoreboard;

public sealed class ScoreboardUIController : UIController, IOnStateEntered<LobbyState>, IOnStateEntered<GameplayState>, IOnStateExited<LobbyState>, IOnStateExited<GameplayState>
{

    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
    private ClientGameTicker _gameTicker = default!;

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
        _gameTicker = _entitySystem.GetEntitySystem<ClientGameTicker>();

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


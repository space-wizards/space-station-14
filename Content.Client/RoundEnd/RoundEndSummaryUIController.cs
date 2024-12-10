using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.RoundEnd;

[UsedImplicitly]
public sealed class RoundEndSummaryUIController : UIController,
    IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private readonly IInputManager _input = default!;

    private RoundEndSummaryWindow? _window;
    private MenuButton? ToggleRoundEndSummaryWindowButton => UIManager.GetActiveUIWidgetOrNull
        <UserInterface.Systems.MenuBar.Widgets.GameTopMenuBar>()?.ToggleRoundEndSummaryWindowButton;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += LoadButton;
        gameplayStateLoad.OnScreenUnload += UnloadButton;
    }

    private void ToggleScoreboardWindow(ICommonSession? session = null)
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
    }

    private void UnloadButton()
    {
        if (ToggleRoundEndSummaryWindowButton == null)
            return;

        ToggleRoundEndSummaryWindowButton.OnPressed -= ActionButtonPressed;
    }

    private void LoadButton()
    {
        if (ToggleRoundEndSummaryWindowButton == null)
            return;

        ToggleRoundEndSummaryWindowButton.OnPressed += ActionButtonPressed;
    }

    private void ActionButtonPressed(BaseButton.ButtonEventArgs args)
    {
        ToggleScoreboardWindow();
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        if (ToggleRoundEndSummaryWindowButton != null)
        {
            ToggleRoundEndSummaryWindowButton.Pressed = true;
            ToggleRoundEndSummaryWindowButton.Visible = true;
        }

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);

        // When the RoundEndSummary Window closes, make sure to remove the "pressed" attribute when the windows closes
        _window.OnClose += () =>
        {
            if (ToggleRoundEndSummaryWindowButton != null)
                ToggleRoundEndSummaryWindowButton.Pressed = false;
        };
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleScoreboardWindow));
    }
}

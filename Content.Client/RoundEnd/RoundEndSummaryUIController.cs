using Content.Client.GameTicking.Managers;
using Content.Client.UserInterface.Controls;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.RoundEnd;

[UsedImplicitly]
public sealed class RoundEndSummaryUIController : UIController,
    IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private readonly IInputManager _input = default!;

    private RoundEndSummaryWindow? _window;

    private MenuButton? SummaryButton;

    public void UnloadButton()
    {
        if (SummaryButton == null)
            return;

        SummaryButton.OnPressed -= SummaryButtonPressed;
    }

    public void LoadButton(MenuButton summaryButton)
    {
        SummaryButton = summaryButton;
        SummaryButton.OnPressed += SummaryButtonPressed;
    }

    private void OnWindowOpen()
    {
        SummaryButton?.SetClickPressed(true);
    }

    public void OnWindowClosed()
    {
        SummaryButton?.SetClickPressed(false);
    }

    public void SummaryButtonPressed(ButtonEventArgs args)
    {
        ToggleScoreboardWindow();
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

    public void ResetSummaryButton()
    {
        if (SummaryButton != null)
            SummaryButton.Visible = false;
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);

        _window.OnOpen += OnWindowOpen;
        _window.OnClose += OnWindowClosed;

        if (SummaryButton != null)
        {
            SummaryButton.Pressed = true;
            SummaryButton.Visible = true;
        }
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow, InputCmdHandler.FromDelegate(ToggleScoreboardWindow));
    }
}

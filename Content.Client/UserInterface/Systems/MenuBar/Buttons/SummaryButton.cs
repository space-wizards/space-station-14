using Content.Client.RoundEnd;
using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.Controllers;
using Content.Client.GameTicking.Managers;
using Content.Shared.GameTicking;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.MenuBar.Buttons;

public sealed class SummaryButtonController : UIController, IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private readonly RoundEndSummaryUIController _summary = default!;

    private MenuButton? _summaryButton;

    public void LoadButton(MenuButton? button)
    {
        if (button == null)
            return;

        _summaryButton = button;
        _summaryButton.OnPressed += OnButtonPressed;
    }

    public void UnloadButton()
    {
        if (_summaryButton == null)
            return;

        _summaryButton.Visible = false;
        _summaryButton.OnPressed -= OnButtonPressed;
    }

    private void OnWindowOpened()
    {
        _summaryButton?.SetClickPressed(true);
    }

    private void OnWindowClosed()
    {
        _summaryButton?.SetClickPressed(false);
    }

    private void OnButtonPressed(BaseButton.ButtonEventArgs args)
    {
        _summary.ToggleScoreboardWindow();
    }

    private void OnRoundEnd(RoundEndMessageEvent message, EntitySessionEventArgs args)
    {
        if (_summaryButton == null || _summary.Window == null)
            return;

        _summaryButton.Visible = true;
        _summaryButton.SetClickPressed(true);
        _summary.Window.OnOpen += OnWindowOpened;
        _summary.Window.OnClose += OnWindowClosed;
    }

    public void OnSystemLoaded(ClientGameTicker system)
    {
        SubscribeNetworkEvent<RoundEndMessageEvent>(OnRoundEnd);
    }
}

using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SpaceArena.UI;

public sealed class SpaceArenaLobbyUIController : UIController, IOnSystemChanged<SpaceArenaLobbyHudSystem>
{
    [UISystemDependency] private readonly SpaceArenaLobbyHudSystem _lobby = default!;

    private Button? _lobbyButton;
    private Button? _returnToHubButton;

    public void LoadButton()
    {
        var menuBar = UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>();
        var lobbyButton = menuBar?.SpaceArenaButton;
        if (_lobbyButton != lobbyButton)
        {
            if (_lobbyButton != null)
                _lobbyButton.OnPressed -= OnLobbyPressed;

            _lobbyButton = lobbyButton;
            if (_lobbyButton != null)
                _lobbyButton.OnPressed += OnLobbyPressed;
        }

        var returnToHubButton = menuBar?.SpaceArenaReturnToHubButton;
        if (_returnToHubButton != returnToHubButton)
        {
            if (_returnToHubButton != null)
                _returnToHubButton.OnPressed -= OnReturnToHubPressed;

            _returnToHubButton = returnToHubButton;
            if (_returnToHubButton != null)
                _returnToHubButton.OnPressed += OnReturnToHubPressed;
        }

        if (_returnToHubButton != null)
            _returnToHubButton.Visible = _lobby.CanReturnToHub;
    }

    public void UnloadButton()
    {
        if (_lobbyButton != null)
            _lobbyButton.OnPressed -= OnLobbyPressed;
        _lobbyButton = null;

        if (_returnToHubButton != null)
            _returnToHubButton.OnPressed -= OnReturnToHubPressed;
        _returnToHubButton = null;
    }

    private void OnLobbyPressed(BaseButton.ButtonEventArgs args)
    {
        UIManager.ClickSound();
        _lobby.RequestOpenLobby();
    }

    public void OnSystemLoaded(SpaceArenaLobbyHudSystem system)
    {
        system.ReturnToHubAvailableChanged += OnReturnToHubAvailableChanged;
    }

    public void OnSystemUnloaded(SpaceArenaLobbyHudSystem system)
    {
        system.ReturnToHubAvailableChanged -= OnReturnToHubAvailableChanged;
    }

    private void OnReturnToHubPressed(BaseButton.ButtonEventArgs args)
    {
        UIManager.ClickSound();
        if (_lobby.IsSpectating)
            _lobby.RequestLeaveSpectating();
        else
            _lobby.RequestLeaveMatch();
    }

    private void OnReturnToHubAvailableChanged(bool available)
    {
        if (_returnToHubButton != null)
            _returnToHubButton.Visible = available;
    }
}

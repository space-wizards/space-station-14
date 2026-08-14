using Content.Client.Eui;
using Content.Shared.Eui;
using Content.Shared.SpaceArena.Components;

namespace Content.Client.SpaceArena.UI;

public sealed class SpaceArenaLobbyEui : BaseEui
{
    private SpaceArenaLobbyMenu? _menu;

    public override void Opened()
    {
        _menu = new SpaceArenaLobbyMenu();
        _menu.OnClose += OnWindowClosed;
        _menu.OnCreateLobby += (mode, arena) =>
            SendMessage(new SpaceArenaCreateLobbyEuiMessage(mode, arena));
        _menu.OnJoinLobby += lobby => SendMessage(new SpaceArenaJoinLobbyEuiMessage(lobby));
        _menu.OnStartLobby += lobby => SendMessage(new SpaceArenaStartLobbyEuiMessage(lobby));
        _menu.OnSpectateLobby += lobby => SendMessage(new SpaceArenaSpectateLobbyEuiMessage(lobby));
        _menu.OnLeaveLobby += () => SendMessage(new SpaceArenaLeaveLobbyEuiMessage());
        _menu.OpenCentered();
    }

    public override void Closed()
    {
        if (_menu == null)
            return;

        _menu.OnClose -= OnWindowClosed;
        _menu.Dispose();
        _menu = null;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SpaceArenaLobbyEuiState lobbyState || _menu == null)
            return;

        _menu.UpdateState(lobbyState.Modes, lobbyState.Arenas, lobbyState.Rooms);
        _menu.UpdateUserStatus(
            lobbyState.CurrentLobby,
            lobbyState.SpectatedMatch,
            lobbyState.CanManageLobbies);
    }

    private void OnWindowClosed()
    {
        SendMessage(new CloseEuiMessage());
    }
}

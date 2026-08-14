using Content.Shared.SpaceArena.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SpaceArena.UI;

[UsedImplicitly]
public sealed class SpaceArenaLobbyBoundUserInterface(EntityUid owner, Enum uiKey)
    : BoundUserInterface(owner, uiKey)
{
    private SpaceArenaLobbyMenu? _menu;

    protected override void Open()
    {
        if (_menu != null)
            return;

        base.Open();

        _menu = this.CreateWindow<SpaceArenaLobbyMenu>();
        _menu.OnCreateLobby += (mode, arena) =>
            SendMessage(new SpaceArenaCreateLobbyMessage(mode, arena));
        _menu.OnJoinLobby += lobby => SendMessage(new SpaceArenaJoinLobbyMessage(lobby));
        _menu.OnStartLobby += lobby => SendMessage(new SpaceArenaStartLobbyMessage(lobby));
        _menu.OnSpectateLobby += lobby =>
            SendMessage(new SpaceArenaSpectateLobbyMessage(lobby));
        _menu.OnLeaveLobby += () => SendMessage(new SpaceArenaLeaveLobbyMessage());
        SendMessage(new SpaceArenaLobbyStatusRequestMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState message)
    {
        base.UpdateState(message);

        if (message is SpaceArenaLobbyBoundUserInterfaceState state)
            _menu?.UpdateState(state);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is SpaceArenaLobbyUserStatusMessage status)
            _menu?.UpdateUserStatus(status.CurrentLobby, status.SpectatedMatch, status.CanManageLobbies);
    }

}

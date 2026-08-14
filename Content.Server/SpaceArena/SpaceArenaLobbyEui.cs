using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.SpaceArena.Components;

namespace Content.Server.SpaceArena;

public sealed class SpaceArenaLobbyEui(
    EntityUid terminal,
    SpaceArenaLobbyTerminalSystem system) : BaseEui
{
    public EntityUid Terminal { get; } = terminal;

    public override void Opened()
    {
        StateDirty();
    }

    public override void Closed()
    {
        system.OnEuiClosed(this);
    }

    public override EuiStateBase GetNewState()
    {
        return system.GetEuiState(Terminal, Player);
    }

    public override void HandleMessage(EuiMessageBase message)
    {
        if (message is CloseEuiMessage)
        {
            base.HandleMessage(message);
            return;
        }

        system.HandleEuiMessage(this, message);
        if (!IsShutDown && Id != 0)
            StateDirty();
    }
}

using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles;

/// <summary>
/// "Waiting for the party to be ready" dialog shown to players who claimed a
/// ghost role party slot. Closing it in any way (cancel button, closing the
/// window, disconnecting) releases the slot, unless the party is already locked
/// in and spawning.
/// </summary>
public sealed class GhostRolePartyWaitingEui : BaseEui
{
    private readonly GhostRolePartySystem _party;
    private readonly EntityUid _controller;

    public GhostRolePartyWaitingEui(GhostRolePartySystem party, EntityUid controller)
    {
        _party = party;
        _controller = controller;
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return _party.GetWaitingState(_controller);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is GhostRolePartyCancelMessage)
            _party.Cancel(_controller, Player);
    }

    public override void Closed()
    {
        base.Closed();
        _party.OnEuiClosed(_controller, Player);
    }
}

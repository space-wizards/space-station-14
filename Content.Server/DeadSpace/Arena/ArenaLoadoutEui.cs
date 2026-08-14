using Content.Server.EUI;
using Content.Shared.DeadSpace.Arena;
using Content.Shared.Eui;
using Robust.Shared.Player;

namespace Content.Server.DeadSpace.Arena;

public sealed class ArenaLoadoutEui : BaseEui
{
    private readonly ArenaSystem _arena;
    private readonly ICommonSession _session;
    public EntityUid SourceGhost { get; }

    public ArenaLoadoutEui(ArenaSystem arena, ICommonSession session, EntityUid sourceGhost)
    {
        _arena = arena;
        _session = session;
        SourceGhost = sourceGhost;
    }

    public override EuiStateBase GetNewState()
    {
        return _arena.GetLoadoutState(_session);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (IsShutDown)
            return;

        switch (msg)
        {
            case ArenaLoadoutSelectedMessage selected:
                _arena.SpawnPlayer(this, _session, SourceGhost, selected.WeaponIndex);
                if (!IsShutDown)
                    Close();
                break;

            case ArenaCostumeBuyMessage buy:
                if (_arena.TryBuyCostume(_session, buy.CostumeIndex))
                    StateDirty();
                break;

            case ArenaCostumeEquipMessage equip:
                _arena.SetEquippedCostumes(_session, equip.CostumeIndexes);
                StateDirty();
                break;
        }
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _arena.OnLoadoutEuiClosed(_session, this);
    }
}

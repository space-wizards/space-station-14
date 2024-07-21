using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Server.Player;

namespace Content.Server.Administration;

public sealed class PlayerPanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private readonly LocatedPlayerData _targetPlayer;
    private int? _notes;
    private int? _bans;
    private int? _roleBans;
    private bool? _whitelisted;
    private TimeSpan _playtime;
    private bool _frozen;
    private bool _canFreeze;
    private bool _canAhelp;

    public PlayerPanelEui(LocatedPlayerData player)
    {
        IoCManager.InjectDependencies(this);
        _targetPlayer = player;
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        return new PlayerPanelEuiState(_targetPlayer.UserId, _targetPlayer.Username, _playtime, _notes, _bans, _roleBans, _whitelisted, _canFreeze, _frozen, _canAhelp);
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player != Player)
            return;

        SetPlayerState();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        // Not sure if it's even possible for them to not be an admin at this point as the bui is set to close when they lose persm
        // I have this just in case tho.
        if (msg is not PlayerPanelFreezeMessage _ || !_admins.IsAdmin(Player) || !_entity.TrySystem<AdminFrozenSystem>(out var frozenSystem))
            return;

        if (_player.TryGetSessionById(_targetPlayer.UserId, out var session) && session.AttachedEntity != null)
        {
            if (_entity.HasComponent<AdminFrozenComponent>(session.AttachedEntity))
            {
                _entity.RemoveComponent<AdminFrozenComponent>(session.AttachedEntity.Value);
            }
            else
            {
                frozenSystem.FreezeAndMute(session.AttachedEntity.Value);
            }
            SetPlayerState();
        }
    }

    public async void SetPlayerState()
    {
        if (!_admins.IsAdmin(Player))
        {
            Close();
            return;
        }

        _playtime = (await _db.GetPlayTimes(_targetPlayer.UserId))
            .Where(p => p.Tracker == "Overall")
            .Select(p => p.TimeSpent)
            .FirstOrDefault();

        if (_notesMan.CanView(Player))
        {
            _notes = (await _notesMan.GetAllAdminRemarks(_targetPlayer.UserId)).Count;
        }
        else
        {
            _notes = null;
        }

        // Apparently the Bans flag is also used for whitelists
        if (_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _whitelisted = await _db.GetWhitelistStatusAsync(_targetPlayer.UserId);
            // This won't get associated ip or hwid bans but they were not placed on this account anyways
            _bans = (await _db.GetServerBansAsync(null, _targetPlayer.UserId, null)).Count;
            _roleBans = (await _db.GetServerRoleBansAsync(null, _targetPlayer.UserId, null)).Count;
        }
        else
        {
            _whitelisted = null;
            _bans = null;
            _roleBans = null;
        }

        if (_player.TryGetSessionById(_targetPlayer.UserId, out var session))
        {
            _canFreeze = session.AttachedEntity != null;
            _frozen = _entity.HasComponent<AdminFrozenComponent>(session.AttachedEntity);
        }
        else
        {
            _canFreeze = false;
        }

        if (_admins.HasAdminFlag(Player, AdminFlags.Adminhelp))
        {
            _canAhelp = true;
        }
        else
        {
            _canAhelp = false;
        }

        StateDirty();
    }
}

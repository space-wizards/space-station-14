using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Notes;
using Content.Server.Administration.Systems;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Eui;
using Content.Shared.Follower;
using Robust.Server.Player;
using Robust.Shared.Player;

namespace Content.Server.Administration;

public sealed class PlayerPanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IAdminNotesManager _notesMan = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    private readonly LocatedPlayerData _targetPlayer;
    private int? _notes;
    private int? _bans;
    private int? _roleBans;
    private int _sharedConnections;
    private bool? _whitelisted;
    private TimeSpan _playtime;
    private bool _frozen;
    private bool _canFreeze;
    private bool _canAhelp;
    private FollowerSystem _follower;

    public PlayerPanelEui(LocatedPlayerData player)
    {
        IoCManager.InjectDependencies(this);
        _targetPlayer = player;
        _follower = _entity.System<FollowerSystem>();
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
        return new PlayerPanelEuiState(_targetPlayer.UserId,
            _targetPlayer.Username,
            _playtime,
            _notes,
            _bans,
            _roleBans,
            _sharedConnections,
            _whitelisted,
            _canFreeze,
            _frozen,
            _canAhelp);
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

        ICommonSession? session;

        switch (msg)
        {
            case PlayerPanelFreezeMessage freezeMsg:
                if (!_admins.IsAdmin(Player) ||
                    !_entity.TrySystem<AdminFrozenSystem>(out var frozenSystem) ||
                    !_player.TryGetSessionById(_targetPlayer.UserId, out session) ||
                    session.AttachedEntity == null)
                    return;

                if (_entity.HasComponent<AdminFrozenComponent>(session.AttachedEntity))
                {
                    _adminLog.Add(LogType.Action,$"{Player:actor} unfroze {_entity.ToPrettyString(session.AttachedEntity):subject}");
                    _entity.RemoveComponent<AdminFrozenComponent>(session.AttachedEntity.Value);
                    SetPlayerState();
                    return;
                }

                if (freezeMsg.Mute)
                {
                    _adminLog.Add(LogType.Action,$"{Player:actor} froze and muted {_entity.ToPrettyString(session.AttachedEntity):subject}");
                    frozenSystem.FreezeAndMute(session.AttachedEntity.Value);
                }
                else
                {
                    _adminLog.Add(LogType.Action,$"{Player:actor} froze {_entity.ToPrettyString(session.AttachedEntity):subject}");
                    _entity.EnsureComponent<AdminFrozenComponent>(session.AttachedEntity.Value);
                }
                SetPlayerState();
                break;

            case PlayerPanelLogsMessage:
                if (!_admins.HasAdminFlag(Player, AdminFlags.Logs))
                    return;

                _adminLog.Add(LogType.Action, $"{Player:actor} opened logs on {_targetPlayer.Username:subject}");
                var ui = new AdminLogsEui();
                _eui.OpenEui(ui, Player);
                ui.SetLogFilter(search: _targetPlayer.Username);
                break;
            case PlayerPanelDeleteMessage:
            case PlayerPanelRejuvenationMessage:
                if (!_admins.HasAdminFlag(Player, AdminFlags.Debug) ||
                    !_player.TryGetSessionById(_targetPlayer.UserId, out session) ||
                    session.AttachedEntity == null)
                    return;

                if (msg is PlayerPanelRejuvenationMessage)
                {
                    _adminLog.Add(LogType.Action,$"{Player:actor} rejuvenated {_entity.ToPrettyString(session.AttachedEntity):subject}");
                    if (!_entity.TrySystem<RejuvenateSystem>(out var rejuvenate))
                        return;

                    rejuvenate.PerformRejuvenate(session.AttachedEntity.Value);
                }
                else
                {
                    _adminLog.Add(LogType.Action,$"{Player:actor} deleted {_entity.ToPrettyString(session.AttachedEntity):subject}");
                    _entity.DeleteEntity(session.AttachedEntity);
                }
                break;
            case PlayerPanelFollowMessage:
                if (!_admins.HasAdminFlag(Player, AdminFlags.Admin) ||
                    !_player.TryGetSessionById(_targetPlayer.UserId, out session) ||
                    session.AttachedEntity == null ||
                    Player.AttachedEntity is null ||
                    session.AttachedEntity == Player.AttachedEntity)
                    return;

                _follower.StartFollowingEntity(Player.AttachedEntity.Value, session.AttachedEntity.Value);
                break;
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

        _sharedConnections = _player.Sessions.Count(s => s.Channel.RemoteEndPoint.Address.Equals(_targetPlayer.LastAddress) && s.UserId != _targetPlayer.UserId);

    // Apparently the Bans flag is also used for whitelists
    if (_admins.HasAdminFlag(Player, AdminFlags.Ban))
        {
            _whitelisted = await _db.GetWhitelistStatusAsync(_targetPlayer.UserId);
            // This won't get associated ip or hwid bans but they were not placed on this account anyways
            _bans = (await _db.GetServerBansAsync(null, _targetPlayer.UserId, null, null)).Count;
            // Unfortunately role bans for departments and stuff are issued individually. This means that a single role ban can have many individual role bans internally
            // The only way to distinguish whether a role ban is the same is to compare the ban time.
            // This is horrible and I would love to just erase the database and start from scratch instead but that's what I can do for now.
            _roleBans = (await _db.GetServerRoleBansAsync(null, _targetPlayer.UserId, null, null)).DistinctBy(rb => rb.BanTime).Count();
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

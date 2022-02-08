using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Content.Server.Chat.Managers;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.Suspicion;
using Content.Server.Suspicion.Roles;
using Content.Server.Traitor.Uplink;
using Content.Server.Traitor.Uplink.Account;
using Content.Shared.CCVar;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking;
using Content.Shared.MobState.Components;
using Content.Shared.Roles;
using Content.Shared.Sound;
using Content.Shared.Suspicion;
using Content.Shared.Traitor.Uplink;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameTicking.Rules;

/// <summary>
///     Simple GameRule that will do a TTT-like gamemode with traitors.
/// </summary>
public sealed class SuspicionRuleSystem : GameRuleSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    public override string Prototype => "Suspicion";

    private static readonly TimeSpan DeadCheckDelay = TimeSpan.FromSeconds(1);

    private readonly HashSet<SuspicionRoleComponent> _traitors = new();

    public IReadOnlyCollection<SuspicionRoleComponent> Traitors => _traitors;

    [DataField("addedSound")] private SoundSpecifier _addedSound = new SoundPathSpecifier("/Audio/Misc/tatoralert.ogg");

    private CancellationTokenSource _checkTimerCancel = new();
    private TimeSpan? _endTime;

    public TimeSpan? EndTime
    {
        get => _endTime;
        set
        {
            _endTime = value;
            SendUpdateToAll();
        }
    }

    public TimeSpan RoundMaxTime { get; set; } = TimeSpan.FromSeconds(CCVars.SuspicionMaxTimeSeconds.DefaultValue);
    public TimeSpan RoundEndDelay { get; set; } = TimeSpan.FromSeconds(10);

    private const string TraitorID = "SuspicionTraitor";
    private const string InnocentID = "SuspicionInnocent";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersAssigned);
        SubscribeLocalEvent<RoundStartAttemptEvent>(OnRoundStartAttempt);
        SubscribeLocalEvent<RefreshLateJoinAllowedEvent>(OnLateJoinRefresh);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

        SubscribeLocalEvent<SuspicionRoleComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SuspicionRoleComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SuspicionRoleComponent, RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<SuspicionRoleComponent, RoleRemovedEvent>(OnRoleRemoved);
    }

    private void OnRoundStartAttempt(RoundStartAttemptEvent ev)
    {
        if (!Enabled)
            return;

        var minPlayers = _cfg.GetCVar(CCVars.SuspicionMinPlayers);

        if (!ev.Forced && ev.Players.Length < minPlayers)
        {
            _chatManager.DispatchServerAnnouncement($"Not enough players readied up for the game! There were {ev.Players.Length} players readied up out of {minPlayers} needed.");
            ev.Cancel();
            return;
        }

        if (ev.Players.Length == 0)
        {
            _chatManager.DispatchServerAnnouncement("No players readied up! Can't start Suspicion.");
            ev.Cancel();
            return;
        }
    }

    private void OnPlayersAssigned(RulePlayerJobsAssignedEvent ev)
    {
        if (!Enabled)
            return;

        var minTraitors = _cfg.GetCVar(CCVars.SuspicionMinTraitors);
        var playersPerTraitor = _cfg.GetCVar(CCVars.SuspicionPlayersPerTraitor);
        var traitorStartingBalance = _cfg.GetCVar(CCVars.SuspicionStartingBalance);

        var list = new List<IPlayerSession>(ev.Players);
        var prefList = new List<IPlayerSession>();

        foreach (var player in list)
        {
            if (!ev.Profiles.ContainsKey(player.UserId) || player.AttachedEntity is not {} attached)
            {
                continue;
            }
            prefList.Add(player);

            attached.EnsureComponent<SuspicionRoleComponent>();
        }

        var numTraitors = MathHelper.Clamp(ev.Players.Length / playersPerTraitor,
            minTraitors, ev.Players.Length);

        var traitors = new List<SuspicionTraitorRole>();

        for (var i = 0; i < numTraitors; i++)
        {
            IPlayerSession traitor;
            if(prefList.Count == 0)
            {
                if (list.Count == 0)
                {
                    Logger.InfoS("preset", "Insufficient ready players to fill up with traitors, stopping the selection.");
                    break;
                }
                traitor = _random.PickAndTake(list);
                Logger.InfoS("preset", "Insufficient preferred traitors, picking at random.");
            }
            else
            {
                traitor = _random.PickAndTake(prefList);
                list.Remove(traitor);
                Logger.InfoS("preset", "Selected a preferred traitor.");
            }
            var mind = traitor.Data.ContentData()?.Mind;
            var antagPrototype = _prototypeManager.Index<AntagPrototype>(TraitorID);

            DebugTools.AssertNotNull(mind?.OwnedEntity);

            var traitorRole = new SuspicionTraitorRole(mind!, antagPrototype);
            mind!.AddRole(traitorRole);
            traitors.Add(traitorRole);

            // creadth: we need to create uplink for the antag.
            // PDA should be in place already, so we just need to
            // initiate uplink account.
            var uplinkAccount = new UplinkAccount(traitorStartingBalance, mind.OwnedEntity!);
            var accounts = EntityManager.EntitySysManager.GetEntitySystem<UplinkAccountsSystem>();
            accounts.AddNewAccount(uplinkAccount);

            // try to place uplink
            if (!EntityManager.EntitySysManager.GetEntitySystem<UplinkSystem>()
                    .AddUplink(mind.OwnedEntity!.Value, uplinkAccount))
                continue;
        }

        foreach (var player in list)
        {
            var mind = player.Data.ContentData()?.Mind;
            var antagPrototype = _prototypeManager.Index<AntagPrototype>(InnocentID);

            DebugTools.AssertNotNull(mind);

            mind!.AddRole(new SuspicionInnocentRole(mind, antagPrototype));
        }

        foreach (var traitor in traitors)
        {
            traitor.GreetSuspicion(traitors, _chatManager);
        }
    }

    public override void Added()
    {
        _playerManager.PlayerStatusChanged += PlayerManagerOnPlayerStatusChanged;

        RoundMaxTime = TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.SuspicionMaxTimeSeconds));

        EndTime = _timing.CurTime + RoundMaxTime;

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-added-announcement"));

        var filter = Filter.Empty()
            .AddWhere(session => ((IPlayerSession) session).ContentData()?.Mind?.HasRole<SuspicionTraitorRole>() ?? false);

        SoundSystem.Play(filter, _addedSound.GetSound(), AudioParams.Default);

        _doorSystem.AccessType = SharedDoorSystem.AccessTypes.AllowAllNoExternal;

        _checkTimerCancel = new CancellationTokenSource();
        Timer.SpawnRepeating(DeadCheckDelay, CheckWinConditions, _checkTimerCancel.Token);
    }

    public override void Removed()
    {
        _doorSystem.AccessType = SharedDoorSystem.AccessTypes.Id;
        EndTime = null;
        _traitors.Clear();

        _playerManager.PlayerStatusChanged -= PlayerManagerOnPlayerStatusChanged;

        _checkTimerCancel.Cancel();
    }

    private void CheckWinConditions()
    {
        if (!Enabled || !_cfg.GetCVar(CCVars.GameLobbyEnableWin))
            return;

        var traitorsAlive = 0;
        var innocentsAlive = 0;

        foreach (var playerSession in _playerManager.ServerSessions)
        {
            if (playerSession.AttachedEntity is not {Valid: true} playerEntity
                || !_entities.TryGetComponent(playerEntity, out MobStateComponent? mobState)
                || !_entities.HasComponent<SuspicionRoleComponent>(playerEntity))
            {
                continue;
            }

            if (!mobState.IsAlive())
            {
                continue;
            }

            var mind = playerSession.ContentData()?.Mind;

            if (mind != null && mind.HasRole<SuspicionTraitorRole>())
                traitorsAlive++;
            else
                innocentsAlive++;
        }

        if (innocentsAlive + traitorsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-stalemate"));
            EndRound(Victory.Stalemate);
        }

        else if (traitorsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-station-win"));
            EndRound(Victory.Innocents);
        }
        else if (innocentsAlive == 0)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-check-winner-traitor-win"));
            EndRound(Victory.Traitors);
        }
        else if (_timing.CurTime > _endTime)
        {
            _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-suspicion-traitor-time-has-run-out"));
            EndRound(Victory.Innocents);
        }
    }

    private enum Victory
    {
        Stalemate,
        Innocents,
        Traitors
    }

    private void EndRound(Victory victory)
    {
        string text;

        switch (victory)
        {
            case Victory.Innocents:
                text = Loc.GetString("rule-suspicion-end-round-innocents-victory");
                break;
            case Victory.Traitors:
                text = Loc.GetString("rule-suspicion-end-round-traitors-victory");
                break;
            default:
                text = Loc.GetString("rule-suspicion-end-round-nobody-victory");
                break;
        }

        GameTicker.EndRound(text);

        _chatManager.DispatchServerAnnouncement(Loc.GetString("rule-restarting-in-seconds", ("seconds", (int) RoundEndDelay.TotalSeconds)));
        _checkTimerCancel.Cancel();

        Timer.Spawn(RoundEndDelay, () => GameTicker.RestartRound());
    }

    private void PlayerManagerOnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.InGame)
        {
            SendUpdateTimerMessage(e.Session);
        }
    }

    private void SendUpdateToAll()
    {
        foreach (var player in _playerManager.ServerSessions.Where(p => p.Status == SessionStatus.InGame))
        {
            SendUpdateTimerMessage(player);
        }
    }

    private void SendUpdateTimerMessage(IPlayerSession player)
    {
        var msg = new SuspicionMessages.SetSuspicionEndTimerMessage
        {
            EndTime = EndTime
        };

        EntityManager.EntityNetManager?.SendSystemNetworkMessage(msg, player.ConnectedClient);
    }

    public void AddTraitor(SuspicionRoleComponent role)
    {
        if (!_traitors.Add(role))
        {
            return;
        }

        foreach (var traitor in _traitors)
        {
            traitor.AddAlly(role);
        }

        role.SetAllies(_traitors);
    }

    public void RemoveTraitor(SuspicionRoleComponent role)
    {
        if (!_traitors.Remove(role))
        {
            return;
        }

        foreach (var traitor in _traitors)
        {
            traitor.RemoveAlly(role);
        }

        role.ClearAllies();
    }

    private void Reset(RoundRestartCleanupEvent ev)
    {
        EndTime = null;
        _traitors.Clear();
    }

    private void OnPlayerDetached(EntityUid uid, SuspicionRoleComponent component, PlayerDetachedEvent args)
    {
        component.SyncRoles();
    }

    private void OnPlayerAttached(EntityUid uid, SuspicionRoleComponent component, PlayerAttachedEvent args)
    {
        component.SyncRoles();
    }

    private void OnRoleAdded(EntityUid uid, SuspicionRoleComponent component, RoleAddedEvent args)
    {
        if (args.Role is not SuspicionRole role) return;
        component.Role = role;
    }

    private void OnRoleRemoved(EntityUid uid, SuspicionRoleComponent component, RoleRemovedEvent args)
    {
        if (args.Role is not SuspicionRole) return;
        component.Role = null;
    }

    private void OnLateJoinRefresh(RefreshLateJoinAllowedEvent ev)
    {
        if (!Enabled)
            return;

        ev.Disallow();
    }
}

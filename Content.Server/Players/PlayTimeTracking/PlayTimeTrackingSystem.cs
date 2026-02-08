using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Afk.Events;
using Content.Server.GameTicking;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Players.PlayTimeTracking;

/// <summary>
/// Connects <see cref="PlayTimeTrackingManager"/> to the simulation state. Reports trackers and such.
/// </summary>
public sealed class PlayTimeTrackingSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tracking.CalcTrackers += CalcTrackers;

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleEvent);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleEvent);
        SubscribeLocalEvent<AFKEvent>(OnAFK);
        SubscribeLocalEvent<UnAFKEvent>(OnUnAFK);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        _adminManager.OnPermsChanged += AdminPermsChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _tracking.CalcTrackers -= CalcTrackers;
        _adminManager.OnPermsChanged -= AdminPermsChanged;
    }

    private void CalcTrackers(ICommonSession player, HashSet<string> trackers)
    {
        if (_afk.IsAfk(player))
            return;

        if (_adminManager.IsAdmin(player))
        {
            trackers.Add(PlayTimeTrackingShared.TrackerAdmin);
            trackers.Add(PlayTimeTrackingShared.TrackerOverall);

            if (!_cfg.GetCVar(CCVars.GameAdminJobTracking))
                return;
        }

        if (!IsPlayerAlive(player))
            return;

        trackers.Add(PlayTimeTrackingShared.TrackerOverall);
        trackers.UnionWith(GetTimedRoles(player));
    }

    /// <summary>
    /// Returns true if the player has an attached mob and it is alive (even if in critical).
    /// </summary>
    private bool IsPlayerAlive(ICommonSession session)
    {
        var attached = session.AttachedEntity;
        if (attached == null)
            return false;

        if (!TryComp<MobStateComponent>(attached, out var state))
            return false;

        return state.CurrentState is MobState.Alive or MobState.Critical;
    }

    public IEnumerable<string> GetTimedRoles(EntityUid mindId)
    {
        foreach (var role in _roles.MindGetAllRoleInfo(mindId))
        {
            if (string.IsNullOrWhiteSpace(role.PlayTimeTrackerId))
                continue;

            yield return _prototypes.Index<PlayTimeTrackerPrototype>(role.PlayTimeTrackerId).ID;
        }
    }

    private IEnumerable<string> GetTimedRoles(ICommonSession session)
    {
        var contentData = _playerManager.GetPlayerData(session.UserId).ContentData();

        if (contentData?.Mind == null)
            return Enumerable.Empty<string>();

        return GetTimedRoles(contentData.Mind.Value);
    }

    private void OnRoleEvent(RoleEvent ev)
    {
        if (_playerManager.TryGetSessionById(ev.Mind.UserId, out var session))
            _tracking.QueueRefreshTrackers(session);
    }

    private void OnRoundEnd(RoundRestartCleanupEvent ev)
    {
        _tracking.Save();
    }

    private void OnUnAFK(ref UnAFKEvent ev)
    {
        _tracking.QueueRefreshTrackers(ev.Session);
    }

    private void OnAFK(ref AFKEvent ev)
    {
        _tracking.QueueRefreshTrackers(ev.Session);
    }

    private void AdminPermsChanged(AdminPermsChangedEventArgs admin)
    {
        _tracking.QueueRefreshTrackers(admin.Player);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        _tracking.QueueRefreshTrackers(ev.Player);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // This doesn't fire if the player doesn't leave their body. I guess it's fine?
        _tracking.QueueRefreshTrackers(ev.Player);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!TryComp(ev.Target, out ActorComponent? actor))
            return;

        _tracking.QueueRefreshTrackers(actor.PlayerSession);
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        _tracking.QueueRefreshTrackers(ev.PlayerSession);
        // Send timers to client when they join lobby, so the UIs are up-to-date.
        _tracking.QueueSendTimers(ev.PlayerSession);
    }

    public void PlayerRolesChanged(ICommonSession player)
    {
        _tracking.QueueRefreshTrackers(player);
    }
}

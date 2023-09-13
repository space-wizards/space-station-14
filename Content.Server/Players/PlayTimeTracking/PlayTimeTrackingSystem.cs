using System.Linq;
using Content.Server.Afk;
using Content.Server.Afk.Events;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Players.PlayTimeTracking;

/// <summary>
/// Connects <see cref="PlayTimeTrackingManager"/> to the simulation state. Reports trackers and such.
/// </summary>
public sealed class PlayTimeTrackingSystem : EntitySystem
{
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly MindSystem _minds = default!;
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;

    public override void Initialize()
    {
        base.Initialize();

        _tracking.CalcTrackers += CalcTrackers;

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdd);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemove);
        SubscribeLocalEvent<AFKEvent>(OnAFK);
        SubscribeLocalEvent<UnAFKEvent>(OnUnAFK);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _tracking.CalcTrackers -= CalcTrackers;
    }

    private void CalcTrackers(IPlayerSession player, HashSet<string> trackers)
    {
        if (_afk.IsAfk(player))
            return;

        if (!IsPlayerAlive(player))
            return;

        trackers.Add(PlayTimeTrackingShared.TrackerOverall);
        trackers.UnionWith(GetTimedRoles(player));
    }

    private bool IsPlayerAlive(IPlayerSession session)
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
        var ev = new MindGetAllRolesEvent(new List<RoleInfo>());
        RaiseLocalEvent(mindId, ref ev);

        foreach (var role in ev.Roles)
        {
            if (string.IsNullOrWhiteSpace(role.PlayTimeTrackerId))
                continue;

            yield return _prototypes.Index<PlayTimeTrackerPrototype>(role.PlayTimeTrackerId).ID;
        }
    }

    private IEnumerable<string> GetTimedRoles(IPlayerSession session)
    {
        var contentData = _playerManager.GetPlayerData(session.UserId).ContentData();

        if (contentData?.Mind == null)
            return Enumerable.Empty<string>();

        return GetTimedRoles(contentData.Mind.Value);
    }

    private void OnRoleRemove(RoleRemovedEvent ev)
    {
        if (_minds.TryGetSession(ev.Mind, out var session))
            _tracking.QueueRefreshTrackers(session);
    }

    private void OnRoleAdd(RoleAddedEvent ev)
    {
        if (_minds.TryGetSession(ev.Mind, out var session))
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

    public bool IsAllowed(IPlayerSession player, string role)
    {
        if (!_prototypes.TryIndex<JobPrototype>(role, out var job) ||
            job.Requirements == null ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var playTimes = _tracking.GetTrackerTimes(player);

        return JobRequirements.TryRequirementsMet(job, playTimes, out _, EntityManager, _prototypes);
    }

    public HashSet<string> GetDisallowedJobs(IPlayerSession player)
    {
        var roles = new HashSet<string>();
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return roles;

        var playTimes = _tracking.GetTrackerTimes(player);

        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (job.Requirements != null)
            {
                foreach (var requirement in job.Requirements)
                {
                    if (JobRequirements.TryRequirementMet(requirement, playTimes, out _, EntityManager, _prototypes))
                        continue;

                    goto NoRole;
                }
            }

            roles.Add(job.ID);
            NoRole:;
        }

        return roles;
    }

    public void RemoveDisallowedJobs(NetUserId userId, ref List<string> jobs)
    {
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return;

        var player = _playerManager.GetSessionByUserId(userId);
        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            // Sorry mate but your playtimes haven't loaded.
            Log.Error($"Playtimes weren't ready yet for {player} on roundstart!");
            playTimes ??= new Dictionary<string, TimeSpan>();
        }

        for (var i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];

            if (!_prototypes.TryIndex<JobPrototype>(job, out var jobber) ||
                jobber.Requirements == null ||
                jobber.Requirements.Count == 0)
                continue;

            foreach (var requirement in jobber.Requirements)
            {
                if (JobRequirements.TryRequirementMet(requirement, playTimes, out _, EntityManager, _prototypes))
                    continue;

                jobs.RemoveSwap(i);
                i--;
                break;
            }
        }
    }

    public void PlayerRolesChanged(IPlayerSession player)
    {
        _tracking.QueueRefreshTrackers(player);
    }
}

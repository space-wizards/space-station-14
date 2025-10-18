using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Afk;
using Content.Server.Afk.Events;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Players.PlayTimeTracking;

/// <summary>
/// Connects <see cref="PlayTimeTrackingManager"/> to the simulation state. Reports trackers and such.
/// </summary>
public sealed class PlayTimeTrackingSystem : EntitySystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
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
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnStationJobsGetCandidates);
        SubscribeLocalEvent<IsRoleAllowedEvent>(OnIsRoleAllowed);
        SubscribeLocalEvent<GetDisallowedJobsEvent>(OnGetDisallowedJobs);
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

    private void OnStationJobsGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        RemoveDisallowedJobs(ev.Player, ev.Jobs);
    }

    private void OnIsRoleAllowed(ref IsRoleAllowedEvent ev)
    {
        if (!IsAllowed(ev.Player, ev.Jobs) || !IsAllowed(ev.Player, ev.Antags))
            ev.Cancelled = true;
    }

    private void OnGetDisallowedJobs(ref GetDisallowedJobsEvent ev)
    {
        ev.Jobs.UnionWith(GetDisallowedJobs(ev.Player));
    }

    private Dictionary<string, TimeSpan>? GetPlayTimesIfEnabled(ICommonSession player)
    {
        Dictionary<string, TimeSpan>? playTimes = null;
        if (_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            if (!_tracking.TryGetTrackerTimes(player, out var outPlayTimes))
            {
                Log.Error($"Unable to check playtimes {Environment.StackTrace}");
                playTimes = new Dictionary<string, TimeSpan>();
            }
            else
            {
                playTimes = outPlayTimes;
            }
        }
        return playTimes;
    }

        /// <summary>
    /// Checks if the player meets role requirements.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="jobs">A list of role prototype IDs</param>
    /// <returns>Returns true if all requirements were met or there were no requirements.</returns>
    public bool IsAllowed(ICommonSession player, List<ProtoId<JobPrototype>>? jobs)
    {
        if (jobs is null)
            return true;

        foreach (var job in jobs)
        {
            if (!IsAllowed(player, job))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the player meets role requirements.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="antags">A list of role prototype IDs</param>
    /// <returns>Returns true if all requirements were met or there were no requirements.</returns>
    public bool IsAllowed(ICommonSession player, List<ProtoId<AntagPrototype>>? antags)
    {
        if (antags is null)
            return true;

        foreach (var antag in antags)
        {
            if (!IsAllowed(player, antag))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the player meets role requirements.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="job">A list of role prototype IDs</param>
    /// <returns>Returns true if all requirements were met or there were no requirements.</returns>
    public bool IsAllowed(ICommonSession player, ProtoId<JobPrototype> job)
    {
        var playTimes = GetPlayTimesIfEnabled(player);

        var allProfilesForJob = _preferencesManager.GetPreferences(player.UserId).GetAllEnabledProfilesForJob(job);
        var requirements = _roles.GetRoleRequirements(job);
        return allProfilesForJob.Values.Any(profile => JobRequirements.TryRequirementsMet(requirements, playTimes, out _, EntityManager, _prototypes, profile));
    }

    /// <summary>
    /// Checks if the player meets role requirements.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="antag">A list of role prototype IDs</param>
    /// <returns>Returns true if all requirements were met or there were no requirements.</returns>
    public bool IsAllowed(ICommonSession player, ProtoId<AntagPrototype> antag)
    {
        var playTimes = GetPlayTimesIfEnabled(player);

        var allProfilesForJob = _preferencesManager.GetPreferences(player.UserId).GetAllEnabledProfilesForAntag(antag);
        var requirements = _roles.GetRoleRequirements(antag);
        return allProfilesForJob.Values.Any(profile => JobRequirements.TryRequirementsMet(requirements, playTimes, out _, EntityManager, _prototypes, profile));
    }

    public HashSet<ProtoId<JobPrototype>> GetDisallowedJobs(ICommonSession player)
    {
        var roles = new HashSet<ProtoId<JobPrototype>>();

        var playTimes = GetPlayTimesIfEnabled(player);

        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            var allProfilesForJob = _preferencesManager.GetPreferences(player.UserId).GetAllEnabledProfilesForJob(job);
            if (allProfilesForJob.Values.All(profile => !JobRequirements.TryRequirementsMet(job, playTimes, out _, EntityManager, _prototypes, profile)))
                roles.Add(job.ID);
        }

        return roles;
    }

    public void RemoveDisallowedJobs(NetUserId userId, List<ProtoId<JobPrototype>> jobs)
    {
        var player = _playerManager.GetSessionById(userId);

        var playTimes = GetPlayTimesIfEnabled(player);

        foreach (var job in jobs.ShallowClone())
        {
            if(!_prototypes.Resolve(job, out var jobToRemove))
                continue;
            var allProfilesForJob = _preferencesManager.GetPreferences(player.UserId).GetAllEnabledProfilesForJob(job);
            if (allProfilesForJob.Values.All(profile =>
                    !JobRequirements.TryRequirementsMet(jobToRemove, playTimes, out _, EntityManager, _prototypes, profile)))
                jobs.Remove(job);
        }
    }

    public void PlayerRolesChanged(ICommonSession player)
    {
        _tracking.QueueRefreshTrackers(player);
    }
}

using Content.Server.GameTicking.Events;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Roles;

/// <summary>
/// Checks job requirements, including playtime requirements if enabled, in response to
/// StationJobsGetCandidatesEvent, IsRoleAllowedEvent, and GetDisallowedJobsEvent
/// </summary>
public sealed class RoleRequirementsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnStationJobsGetCandidates);
        SubscribeLocalEvent<IsRoleAllowedEvent>(OnIsRoleAllowed);
        SubscribeLocalEvent<GetDisallowedJobsEvent>(OnGetDisallowedJobs);
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
        Dictionary<string, TimeSpan>? playTimes = null;
        if (_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            if (!_tracking.TryGetTrackerTimes(player, out playTimes))
            {
                Log.Error($"Unable to check playtimes {Environment.StackTrace}");
                playTimes = new Dictionary<string, TimeSpan>();
            }
        }

        var requirements = _roles.GetRoleRequirements(job);
        return JobRequirements.TryRequirementsMet(
            requirements,
            playTimes,
            out _,
            EntityManager,
            _prototypes,
            (HumanoidCharacterProfile?)
            _preferencesManager.GetPreferences(player.UserId).SelectedCharacter);
    }

    /// <summary>
    /// Checks if the player meets role requirements.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="antag">A list of role prototype IDs</param>
    /// <returns>Returns true if all requirements were met or there were no requirements.</returns>
    public bool IsAllowed(ICommonSession player, ProtoId<AntagPrototype> antag)
    {
        Dictionary<string, TimeSpan>? playTimes = null;
        if (_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            if (!_tracking.TryGetTrackerTimes(player, out playTimes))
            {
                Log.Error($"Unable to check playtimes {Environment.StackTrace}");
                playTimes = new Dictionary<string, TimeSpan>();
            }
        }

        var requirements = _roles.GetRoleRequirements(antag);
        return JobRequirements.TryRequirementsMet(
            requirements,
            playTimes,
            out _,
            EntityManager,
            _prototypes,
            (HumanoidCharacterProfile?)
            _preferencesManager.GetPreferences(player.UserId).SelectedCharacter);
    }

    public HashSet<ProtoId<JobPrototype>> GetDisallowedJobs(ICommonSession player)
    {
        var roles = new HashSet<ProtoId<JobPrototype>>();
        Dictionary<string, TimeSpan>? playTimes = null;
        if (_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            if (!_tracking.TryGetTrackerTimes(player, out playTimes))
            {
                Log.Error($"Unable to check playtimes {Environment.StackTrace}");
                playTimes = new Dictionary<string, TimeSpan>();
            }
        }

        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (JobRequirements.TryRequirementsMet(job, playTimes, out _, EntityManager, _prototypes, (HumanoidCharacterProfile?) _preferencesManager.GetPreferences(player.UserId).SelectedCharacter))
                roles.Add(job.ID);
        }

        return roles;
    }

    public void RemoveDisallowedJobs(NetUserId userId, List<ProtoId<JobPrototype>> jobs)
    {
        Dictionary<string, TimeSpan>? playTimes = null;
        if (_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            var player = _playerManager.GetSessionById(userId);
            if (!_tracking.TryGetTrackerTimes(player, out playTimes))
            {
                // Sorry mate but your playtimes haven't loaded.
                Log.Error($"Playtimes weren't ready yet for {player} on roundstart!");
                playTimes = new Dictionary<string, TimeSpan>();
            }
        }

        for (var i = 0; i < jobs.Count; i++)
        {
            if (_prototypes.Resolve(jobs[i], out var job)
                && JobRequirements.TryRequirementsMet(job, playTimes, out _, EntityManager, _prototypes, (HumanoidCharacterProfile?) _preferencesManager.GetPreferences(userId).SelectedCharacter))
            {
                continue;
            }

            jobs.RemoveSwap(i);
            i--;
        }
    }
}

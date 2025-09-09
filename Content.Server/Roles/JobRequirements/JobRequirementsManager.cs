using System.Diagnostics.CodeAnalysis;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Preferences.Managers;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Serilog;

namespace Content.Server.Roles;

public sealed class JobRequirementsManager : ISharedJobRequirementsManager
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly PlayTimeTrackingManager _tracking = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IServerPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public bool IsAllowed(ICommonSession session, ProtoId<JobPrototype> jobId, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        profile = (HumanoidCharacterProfile?)_preferencesManager.GetPreferences(session.UserId).SelectedCharacter;
        reason = null;

        if (!_prototypes.TryIndex(jobId, out var job) ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        if (!_tracking.TryGetTrackerTimes(session, out var playTimes))
        {
            Log.Error($"Unable to check playtimes {Environment.StackTrace}");
            playTimes = new Dictionary<string, TimeSpan>();
        }

        return JobRequirements.TryRequirementsMet(job, playTimes, out reason, _entity, _prototypes, profile);
    }

    public void RemoveDisallowedJobs(NetUserId userId, List<ProtoId<JobPrototype>> jobs)
    {
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return;

        var player = _playerManager.GetSessionById(userId);
        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            // Sorry mate but your playtimes haven't loaded.
            Log.Error($"Playtimes weren't ready yet for {player} on roundstart!");
            playTimes ??= new Dictionary<string, TimeSpan>();
        }

        for (var i = 0; i < jobs.Count; i++)
        {
            if (_prototypes.TryIndex(jobs[i], out var job)
                && JobRequirements.TryRequirementsMet(job, playTimes, out _, _entity, _prototypes, (HumanoidCharacterProfile?) _preferencesManager.GetPreferences(userId).SelectedCharacter))
            {
                continue;
            }

            jobs.RemoveSwap(i);
            i--;
        }
    }

    public HashSet<ProtoId<JobPrototype>> GetDisallowedJobs(ICommonSession player)
    {
        var roles = new HashSet<ProtoId<JobPrototype>>();
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return roles;

        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            Log.Error($"Unable to check playtimes {Environment.StackTrace}");
            playTimes = new Dictionary<string, TimeSpan>();
        }

        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (JobRequirements.TryRequirementsMet(job, playTimes, out _, _entity, _prototypes, (HumanoidCharacterProfile?) _preferencesManager.GetPreferences(player.UserId).SelectedCharacter))
                roles.Add(job.ID);
        }

        return roles;
    }
}

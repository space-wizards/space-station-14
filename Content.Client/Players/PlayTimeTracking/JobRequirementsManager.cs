using System.Diagnostics.CodeAnalysis;
using Content.Client.Administration.Managers;
using Content.Client.Preferences;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Preferences;
using Content.Shared.Humanoid.Prototypes;
using ReasonList = System.Collections.Generic.List<string>;

namespace Content.Client.Players.PlayTimeTracking;

public sealed class JobRequirementsManager
{
    [Dependency] private readonly IClientPreferencesManager _preferencesManager = default!;
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IClientAdminManager _adminManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private readonly Dictionary<string, TimeSpan> _roles = new();
    private readonly List<string> _roleBans = new();

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("job_requirements");

        // Yeah the client manager handles role bans and playtime but the server ones are separate DEAL.
        _net.RegisterNetMessage<MsgRoleBans>(RxRoleBans);
        _net.RegisterNetMessage<MsgPlayTime>(RxPlayTime);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _roles.Clear();
        }
    }

    private void RxRoleBans(MsgRoleBans message)
    {
        _sawmill.Debug($"Received roleban info containing {message.Bans.Count} entries.");

        if (_roleBans.Equals(message.Bans))
            return;

        _roleBans.Clear();
        _roleBans.AddRange(message.Bans);
        Updated?.Invoke();
    }

    private void RxPlayTime(MsgPlayTime message)
    {
        _roles.Clear();

        // NOTE: do not assign _roles = message.Trackers due to implicit data sharing in integration tests.
        foreach (var (tracker, time) in message.Trackers)
        {
            _roles[tracker] = time;
        }

        /*var sawmill = Logger.GetSawmill("play_time");
        foreach (var (tracker, time) in _roles)
        {
            sawmill.Info($"{tracker}: {time}");
        }*/
        Updated?.Invoke();
    }

    //SS220-Client-admin-check-for-jobs
    private bool IsBypassedChecks()
    {
        return _adminManager.IsActive();
    }

    public bool IsAllowed(JobPrototype job, HumanoidCharacterProfile profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (_roleBans.Contains($"Job:{job.ID}"))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));
            return false;
        }

        if (job.Requirements == null ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            return true;
        }

        var player = _playerManager.LocalPlayer?.Session;
        if (player == null)
            return true;

        return CheckAllowed(job, profile, out reason);
    }

    //SS220 Species-Job-Requirement begin
    public void BuildReason(ReasonList reasons, out FormattedMessage reason)
    {
        reason = FormattedMessage.FromMarkup(string.Join('\n', reasons));
    }

    public bool CheckAllowed(JobPrototype job, HumanoidCharacterProfile profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        ReasonList reasons = new();

        var bCheckSpecies = CheckSpeciesRestrict(job, profile, reasons);
        var bCheckRoleTime = CheckRoleTime(job.Requirements, reasons);

        if (bCheckSpecies && bCheckRoleTime)
        {
            return true;
        }

        BuildReason(reasons, out reason);
        return false;
    }

    public bool CheckSpeciesRestrict(JobPrototype job, HumanoidCharacterProfile profile, ReasonList reasons)
    {
        var species = _prototypeManager.Index<SpeciesPrototype>(profile.Species);

        if (species is not null)
        {
            if (JobRequirements.TryRequirementsSpeciesMet(job, species, out var reason, _prototypeManager))
                return true;

            reasons.Add(reason.ToMarkup());
            return false;
        }

        return true;
    }

    // for compatible things like xaml for ghost roles
    public bool CheckRoleTime(HashSet<JobRequirement>? requirements, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        ReasonList reasons = new();

        if (!CheckRoleTime(requirements, reasons))
        {
            BuildReason(reasons, out reason);
            return false;
        }
        return true;
    }
    //SS220 Species-Job-Requirement end

    public bool CheckRoleTime(HashSet<JobRequirement>? requirements, ReasonList reasons)
    {
        if (requirements == null)
            return true;

        foreach (var requirement in requirements)
        {
            if (JobRequirements.TryRequirementMet(requirement, _roles, out var jobReason, _entManager, _prototypes))
                continue;

            //SS220-Client-admin-check-for-jobs
            if (IsBypassedChecks())
                continue;

            reasons.Add(jobReason.ToMarkup());
        }

        return reasons.Count == 0; //SS220 Species-Job-Requirement
    }

    public TimeSpan FetchOverallPlaytime()
    {
        return _roles.TryGetValue("Overall", out var overallPlaytime) ? overallPlaytime : TimeSpan.Zero;
    }

    public IEnumerable<KeyValuePair<string, TimeSpan>> FetchPlaytimeByRoles()
    {
        var jobsToMap = _prototypes.EnumeratePrototypes<JobPrototype>();

        foreach (var job in jobsToMap)
        {
            if (_roles.TryGetValue(job.PlayTimeTracker, out var locJobName))
            {
                yield return new KeyValuePair<string, TimeSpan>(job.Name, locJobName);
            }
        }
    }


}

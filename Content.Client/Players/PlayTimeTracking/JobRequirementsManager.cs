using System.Diagnostics.CodeAnalysis;
using Content.Client.Lobby;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Players.PlayTimeTracking;

// TODO: Should be RoleRequirementsManager but this change needs to be done independently.
public sealed class JobRequirementsManager : ISharedPlaytimeManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly Dictionary<string, TimeSpan> _roles = new();
    private readonly List<ProtoId<JobPrototype>> _jobBans = new();
    private readonly List<ProtoId<AntagPrototype>> _antagBans = new();
    private readonly List<string> _jobWhitelists = new();

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("job_requirements");

        // Yeah the client manager handles role bans and playtime but the server ones are separate DEAL.
        _net.RegisterNetMessage<MsgRoleBans>(RxRoleBans);
        _net.RegisterNetMessage<MsgPlayTime>(RxPlayTime);
        _net.RegisterNetMessage<MsgJobWhitelist>(RxJobWhitelist);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _roles.Clear();
            _jobWhitelists.Clear();
            _jobBans.Clear();
            _antagBans.Clear();
        }
    }

    private void RxRoleBans(MsgRoleBans message)
    {
        _sawmill.Debug($"Received roleban info containing {message.JobBans.Count} job ban entries and {message.AntagBans.Count} antag ban entries.");

        _jobBans.Clear();
        _jobBans.AddRange(message.JobBans);
        _antagBans.Clear();
        _antagBans.AddRange(message.AntagBans);

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

    private void RxJobWhitelist(MsgJobWhitelist message)
    {
        _jobWhitelists.Clear();
        _jobWhitelists.AddRange(message.Whitelist);
        Updated?.Invoke();
    }

    public bool IsAllowed(RolePrototype role, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var banned = role switch
        {
            JobPrototype => _jobBans.Contains(role.ID),
            AntagPrototype => _antagBans.Contains(role.ID),
            _ => false,
        };

        if (banned)
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));

            return false;
        }

        if (!CheckWhitelist(role, out reason))
            return false;

        return _playerManager.LocalSession is null || CheckRoleRequirements(role, profile, out reason);
    }

    public bool CheckRoleRequirements(RolePrototype role, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        return CheckRoleRequirements(_entManager.System<SharedRoleSystem>().GetRoleRequirement(role), profile, out reason);
    }

    public bool CheckRoleRequirements(HashSet<JobRequirement>? requirements, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (requirements == null || !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var reasons = new List<string>();
        foreach (var requirement in requirements)
        {
            if (requirement.Check(_entManager, _prototypes, profile, _roles, out var jobReason))
                continue;

            reasons.Add(jobReason.ToMarkup());
        }

        reason = reasons.Count == 0 ? null : FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        return reason == null;
    }

    public bool CheckWhitelist(RolePrototype role, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        if (!role.Whitelisted || _jobWhitelists.Contains(role.ID))
            return true;

        reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));

        return false;
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

    public IReadOnlyDictionary<string, TimeSpan> GetPlayTimes(ICommonSession session)
    {
        if (session != _playerManager.LocalSession)
        {
            return new Dictionary<string, TimeSpan>();
        }

        return _roles;
    }
}

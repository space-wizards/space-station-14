using System.Diagnostics.CodeAnalysis;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Content.Shared.Players.JobWhitelist;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Roles;

public sealed class JobRequirementsManager : ISharedJobRequirementsManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    private readonly List<string> _roleBans = new();
    private readonly List<string> _jobWhitelists = new();

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("job_requirements");

        // Yeah the client manager handles role bans and playtime but the server ones are separate DEAL.
        _net.RegisterNetMessage<MsgRoleBans>(RxRoleBans);
        _net.RegisterNetMessage<MsgJobWhitelist>(RxJobWhitelist);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
        _playTimeTracking.Updated += Updated;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel == ClientRunLevel.Initialize)
        {
            // Reset on disconnect, just in case.
            _jobWhitelists.Clear();
            _roleBans.Clear();
        }
    }

    private void RxRoleBans(MsgRoleBans message)
    {
        _sawmill.Debug($"Received roleban info containing {message.Bans.Count} entries.");

        _roleBans.Clear();
        _roleBans.AddRange(message.Bans);
        Updated?.Invoke();
    }

    private void RxJobWhitelist(MsgJobWhitelist message)
    {
        _jobWhitelists.Clear();
        _jobWhitelists.AddRange(message.Whitelist);
        Updated?.Invoke();
    }

    public bool IsAllowed(ICommonSession session, ProtoId<JobPrototype> jobId, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (_roleBans.Contains($"Job:{jobId.Id}"))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-ban"));
            return false;
        }

        if (!CheckWhitelist(jobId, out reason))
            return false;

        return CheckRoleRequirements(jobId, profile, out reason);
    }

    public bool CheckRoleRequirements(ProtoId<JobPrototype> jobId, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        var reqs = _entManager.System<SharedRoleSystem>().GetJobRequirement(jobId);
        return CheckRoleRequirements(reqs, profile, out reason);
    }

    public bool CheckRoleRequirements(HashSet<JobRequirement>? requirements, HumanoidCharacterProfile? profile, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (requirements == null || !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var reasons = new List<string>();
        foreach (var requirement in requirements)
        {
            if (requirement.Check(_entManager, _prototypes, profile, _playTimeTracking.Roles, out var jobReason))
                continue;

            reasons.Add(jobReason.ToMarkup());
        }

        reason = reasons.Count == 0 ? null : FormattedMessage.FromMarkupOrThrow(string.Join('\n', reasons));
        return reason == null;
    }

    public bool CheckWhitelist(ProtoId<JobPrototype> jobId, [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = default;
        if (!_cfg.GetCVar(CCVars.GameRoleWhitelist))
            return true;

        var job = _prototypes.Index(jobId);
        if (job.Whitelisted && !_jobWhitelists.Contains(jobId.Id))
        {
            reason = FormattedMessage.FromUnformatted(Loc.GetString("role-not-whitelisted"));
            return false;
        }

        return true;
    }
}

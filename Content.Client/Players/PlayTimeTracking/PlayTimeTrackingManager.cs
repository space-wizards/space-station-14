using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Client.Administration.Managers;
using Content.Shared.CCVar;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Client;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.Players.PlayTimeTracking;

public sealed class PlayTimeTrackingManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IClientNetManager _net = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IClientAdminManager _admin = default!;

    private readonly Dictionary<string, TimeSpan> _roles = new();

    public void Initialize()
    {
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
    }

    public bool IsAllowed(JobPrototype job, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (job.Requirements == null ||
            _cfg.GetCVar(CCVars.GameRoleTimersAdminBypass) && _admin.IsActive(true) ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
        {
            return true;
        }

        var player = _playerManager.LocalPlayer?.Session;

        if (player == null)
            return true;

        var reasonBuilder = new StringBuilder();

        foreach (var requirement in job.Requirements)
        {
            if (JobRequirements.TryRequirementMet(requirement, _roles, out reason, _prototypes))
                continue;

            reasonBuilder.AppendLine(reason);
        }

        reason = reasonBuilder.Length == 0 ? null : reasonBuilder.ToString().TrimEnd('\n');
        return reason == null;
    }
}

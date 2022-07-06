using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Client.Player;
using Robust.Shared.Players;

namespace Content.Client.Roles;

public sealed class RoleTimerSystem : SharedRoleTimerSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public TimeSpan OverallPlaytime { get; private set; }
    private Dictionary<string, TimeSpan> _roles = new();

    // TODO: Hook the UI up to this.
    public event Action? ReceivedRoleTimes;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoleTimersEvent>(OnRoleTimers);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        // Clear out if we connect somewhere else.
        OverallPlaytime = TimeSpan.Zero;
        _roles.Clear();
    }

    private void OnRoleTimers(RoleTimersEvent ev)
    {
        Sawmill.Info($"Received role timers from server, overall is {ev.Overall} and found {ev.RoleTimes.Count} roles");
        OverallPlaytime = ev.Overall;
        _roles = ev.RoleTimes;
    }

    public bool IsAllowed(JobPrototype job, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (job.Requirements == null ||
            !ConfigManager.GetCVar(CCVars.GameRoleTimers)) return true;

        var player = _playerManager.LocalPlayer?.Session;

        if (player == null) return true;

        var overall = OverallPlaytime;
        var roles = _roles;
        var reasonBuilder = new StringBuilder();

        foreach (var requirement in job.Requirements)
        {
            reason = RequirementMet(player, job, requirement, overall, roles);

            if (reason == null) continue;
            reasonBuilder.AppendLine(reason);
        }

        reason = reasonBuilder.Length == 0 ? null : reasonBuilder.ToString();
        return reason == null;
    }

    protected override TimeSpan GetOverallPlaytime(ICommonSession session)
    {
        return OverallPlaytime;
    }

    protected override Dictionary<string, TimeSpan> GetRolePlaytime(ICommonSession session, string role)
    {
        return _roles;
    }
}

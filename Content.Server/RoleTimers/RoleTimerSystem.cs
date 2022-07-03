using System.Diagnostics.CodeAnalysis;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.RoleTimers;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoleTimerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly RoleTimerManager _roleTimers = default!;

    // Autosave regularly just in case of job changes or whatever; it's much easier and more generous.
    private const float AutosaveDelay = 300;
    private float _autoSaveAccumulator = AutosaveDelay;

    /// <summary>
    /// If someone just joined track the last time we set their times so the autosave doesn't round up.
    /// </summary>
    private Dictionary<IPlayerSession, TimeSpan> _lastSetTime = new();

    public override void Initialize()
    {
        base.Initialize();

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                _lastSetTime[args.Session] = _timing.CurTime;
                break;
            case SessionStatus.Disconnected:
            {
                _lastSetTime.Remove(args.Session);
                break;
            }
        }
    }

    private void OnRoundEnd(RoundRestartCleanupEvent ev)
    {
        _autoSaveAccumulator = AutosaveDelay;
    }

    public override void Update(float frameTime)
    {
        if (_ticker.RunLevel != GameRunLevel.InRound) return;

        _autoSaveAccumulator -= frameTime;

        if (_autoSaveAccumulator > 0f) return;

        _autoSaveAccumulator += AutosaveDelay;
        AutoSave();
    }

    private void AutoSave()
    {
        var currentTime = _timing.CurTime;

        // This is gonna have rounding if someone changes their jobs but it's only 5 minutes anyway, not like we need to track
        // per second values every time they get a new job.
        foreach (var player in Filter.GetAllPlayers())
        {
            var pSession = (IPlayerSession) player;
            Save(pSession, currentTime);
        }
    }

    private void Save(IPlayerSession pSession, TimeSpan currentTime)
    {
        _lastSetTime.TryGetValue(pSession, out var lastSave);
        var addedTime = currentTime - lastSave;

        var roles = GetRoles(pSession);

        foreach (var role in roles)
        {
            _roleTimers.AddTimeToRole(pSession.UserId, role, addedTime);
        }

        _roleTimers.AddTimeToOverallPlaytime(pSession.UserId, addedTime);
        _lastSetTime[pSession] = currentTime;
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        PlayerRolesChanged(ev.Player);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        // This doesn't fire if the player doesn't leave their body. I guess it's fine?
        PlayerRolesChanged(ev.Player);
    }

    /// <summary>
    /// Does the player meet the role timer requirements for a particular role?
    /// </summary>
    public bool IsAllowed(IPlayerSession pSession, string role, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (!_configManager.GetCVar(CCVars.GameRoleTimers) ||
            !_protoManager.TryIndex<JobPrototype>(role, out var job) ||
            job.Requirements == null ||
            job.Requirements.Count == 0) return true;

        TimeSpan? overallPlaytime;
        Dictionary<string, TimeSpan>? rolePlaytimes;

        foreach (var requirement in job.Requirements)
        {
            switch (requirement)
            {
                case DepartmentTimeRequirement deptRequirement:
                    rolePlaytimes = _roleTimers.GetRolePlaytimes(pSession).Result;
                    break;
                case OverallPlaytimeRequirement overallRequirement:
                    overallPlaytime = _roleTimers.GetOverallPlaytime(pSession).Result;
                    break;
                case RoleTimeRequirement roleRequirement:
                    rolePlaytimes = _roleTimers.GetRolePlaytimes(pSession).Result;
                    break;
            }

            reason = "a";
            return false;
        }

        return true;
    }

    private List<string> GetRoles(IPlayerSession session)
    {
        var roles = new List<string>();
        var contentData = _playerManager.GetPlayerData(session.UserId).ContentData();

        if (contentData?.Mind == null) return roles;

        foreach (var mindRole in contentData.Mind.AllRoles)
        {
            roles.Add(mindRole.Name);
        }

        return roles;
    }

    private void PlayerRolesChanged(IPlayerSession player)
    {
        Save(player, _timing.CurTime);
    }
}

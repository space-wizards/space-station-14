using System.Diagnostics.CodeAnalysis;
using Content.Server.Afk;
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
using Robust.Shared.Utility;

namespace Content.Server.RoleTimers;

/// <summary>
/// This handles...
/// </summary>
public sealed class RoleTimerSystem : EntitySystem
{
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly RoleTimerManager _roleTimers = default!;

    /// <summary>
    /// Autosave regularly in case of server crash.
    /// </summary>
    private const float AutosaveDelay = 900;

    private float _autoSaveAccumulator = 0f;

    /// <summary>
    /// If someone just joined track the last time we set their times so the autosave doesn't round up.
    /// </summary>
    private readonly Dictionary<IPlayerSession, TimeSpan> _lastSetTime = new();

    public override void Initialize()
    {
        base.Initialize();

        // TODO: This is gonna have ordering bugs with RoleTimerManager so sort that out mate.
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        FullSave();
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
        FullSave();
    }

    public override void Update(float frameTime)
    {
        if (_ticker.RunLevel != GameRunLevel.InRound) return;

        _autoSaveAccumulator += frameTime;

        if (_autoSaveAccumulator < AutosaveDelay) return;

        _autoSaveAccumulator -= AutosaveDelay;
        FullSave();
    }

    // TODO: AFK Events
    // TODO: Mind role events?

    private void OnAfk(IPlayerSession pSession)
    {
        Save(pSession, _timing.CurTime);
    }

    private void OnUnafk(IPlayerSession pSession)
    {
        _lastSetTime[pSession] = _timing.CurTime;
    }

    private void FullSave()
    {
        var currentTime = _timing.CurTime;

        // This is gonna have rounding if someone changes their jobs but it's only 5 minutes anyway, not like we need to track
        // per second values every time they get a new job.
        foreach (var player in Filter.GetAllPlayers())
        {
            var pSession = (IPlayerSession) player;
            if (_afk.IsAfk(pSession)) continue;
            Save(pSession, currentTime);
        }
    }

    private void Save(IPlayerSession pSession, TimeSpan currentTime)
    {
        if (!_lastSetTime.TryGetValue(pSession, out var lastSave))
        {
            lastSave = currentTime;
        }

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

    public bool IsAllowed(IPlayerSession pSession, string role)
    {
        if (!_protoManager.TryIndex<JobPrototype>(role, out var job) ||
            job.Requirements == null ||
            !_configManager.GetCVar(CCVars.GameRoleTimers)) return true;

        var overall = _roleTimers.GetOverallPlaytime(pSession.UserId).Result;
        var roleTime = _roleTimers.GetPlayTimeForRole(pSession.UserId, role).Result;

        foreach (var requirement in job.Requirements)
        {
            // TODO
            return false;
        }

        return true;
    }

    /// <summary>
    /// Does the player meet the job requirements for a particular role?
    /// </summary>
    public bool IsAllowed(IPlayerSession pSession, string role, TimeSpan overallPlaytime, Dictionary<string, TimeSpan> rolePlaytimes, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (!_configManager.GetCVar(CCVars.GameRoleTimers) ||
            !_protoManager.TryIndex<JobPrototype>(role, out var job) ||
            job.Requirements == null ||
            job.Requirements.Count == 0) return true;

        foreach (var requirement in job.Requirements)
        {
            switch (requirement)
            {
                case DepartmentTimeRequirement deptRequirement:
                    break;
                case OverallPlaytimeRequirement overallRequirement:
                    break;
                case RoleTimeRequirement roleRequirement:
                    break;
            }

            reason = "a";
            return false;
        }

        return true;
    }

    public HashSet<string> GetDisAllowedJobs(NetUserId netUserId)
    {
        var roles = new HashSet<string>();
        if (!_configManager.GetCVar(CCVars.GameRoleTimers)) return roles;

        // TODO:
        return roles;
        TimeSpan? overallPlaytime = null;
        Dictionary<string, TimeSpan>? rolePlaytimes = null;

        foreach (var job in _protoManager.EnumeratePrototypes<JobPrototype>())
        {
            if (job.Requirements != null)
            {
                foreach (var requirement in job.Requirements)
                {
                    switch (requirement)
                    {
                        case DepartmentTimeRequirement deptRequirement:
                            rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                            break;
                        case OverallPlaytimeRequirement overallRequirement:
                            overallPlaytime ??= _roleTimers.GetOverallPlaytime(netUserId).Result;
                            break;
                        case RoleTimeRequirement roleRequirement:
                            rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                            break;
                    }
                }
            }

            roles.Add(job.ID);
        }

        return roles;
    }

    public List<string> GetAllowedJobs(NetUserId netUserId)
    {
        var roleTimers = _configManager.GetCVar(CCVars.GameRoleTimers);

        TimeSpan? overallPlaytime = null;
        Dictionary<string, TimeSpan>? rolePlaytimes = null;
        var roles = new List<string>();

        foreach (var job in _protoManager.EnumeratePrototypes<JobPrototype>())
        {
            if (roleTimers && job.Requirements != null)
            {
                foreach (var requirement in job.Requirements)
                {
                    switch (requirement)
                    {
                        case DepartmentTimeRequirement deptRequirement:
                            rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                            break;
                        case OverallPlaytimeRequirement overallRequirement:
                            overallPlaytime ??= _roleTimers.GetOverallPlaytime(netUserId).Result;
                            break;
                        case RoleTimeRequirement roleRequirement:
                            rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                            break;
                    }
                }
            }

            roles.Add(job.ID);
        }

        return roles;
    }

    public void SetAllowedJobs(NetUserId netUserId, ref List<string> jobs)
    {
        if (!_configManager.GetCVar(CCVars.GameRoleTimers)) return;

        TimeSpan? overallPlaytime = null;
        Dictionary<string, TimeSpan>? rolePlaytimes = null;

        for (var i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];

            if (!_protoManager.TryIndex<JobPrototype>(job, out var jobber) ||
                jobber.Requirements == null ||
                jobber.Requirements.Count == 0) continue;

            foreach (var requirement in jobber.Requirements)
            {
                switch (requirement)
                {
                    case DepartmentTimeRequirement deptRequirement:
                        rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                        break;
                    case OverallPlaytimeRequirement overallRequirement:
                        overallPlaytime ??= _roleTimers.GetOverallPlaytime(netUserId).Result;
                        break;
                    case RoleTimeRequirement roleRequirement:
                        rolePlaytimes ??= _roleTimers.GetRolePlaytimes(netUserId).Result;
                        break;
                }

                jobs.RemoveSwap(i);
                i--;
            }
        }
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

    public void PlayerRolesChanged(IPlayerSession player)
    {
        Save(player, _timing.CurTime);
    }
}

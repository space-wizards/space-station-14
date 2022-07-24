using System.Linq;
using Content.Server.Afk;
using Content.Server.Afk.Events;
using Content.Server.Players;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Roles;

/// <summary>
/// This handles issuing saves of role / overall times to the DB during the regular course of play.
/// <see cref="PlayTimeTrackingManager"/> handles the actual data.
/// </summary>
public sealed class RoleTimerSystem : EntitySystem
{
    [Dependency] private readonly IAfkManager _afk = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    private ISawmill _sawmill = default!;

    /// <summary>
    /// If someone just joined track the last time we set their times so the autosave doesn't round up.
    /// </summary>
    private readonly Dictionary<IPlayerSession, TimeSpan> _lastSetTime = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("play_time");

        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _playTimeTracking.BeforeSave += FullSave;

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdd);
        SubscribeLocalEvent<RoleRemovedEvent>(OnRoleRemove);
        SubscribeLocalEvent<AFKEvent>(OnAFK);
        SubscribeLocalEvent<UnAFKEvent>(OnUnAFK);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        FullSave();

        _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;
        _playTimeTracking.BeforeSave -= FullSave;
    }


    public IEnumerable<string> GetTimedRoles(Mind.Mind mind)
    {
        foreach (var role in mind.AllRoles)
        {
            if (role is not IRoleTimer timer)
                continue;

            yield return _prototypes.Index<PlayTimeTrackerPrototype>(timer.Timer).ID;
        }
    }

    private IEnumerable<string> GetTimedRoles(IPlayerSession session)
    {
        var contentData = _playerManager.GetPlayerData(session.UserId).ContentData();

        if (contentData?.Mind == null)
            return Enumerable.Empty<string>();

        return GetTimedRoles(contentData.Mind);
    }

    private void OnRoleRemove(RoleRemovedEvent ev)
    {
        if (ev.Mind.Session == null)
            return;

        Save(ev.Mind.Session);
    }

    private void OnRoleAdd(RoleAddedEvent ev)
    {
        if (ev.Mind.Session == null)
            return;

        var time = "";
        if (ev.Role is IRoleTimer timer)
            time = _prototypes.Index<PlayTimeTrackerPrototype>(timer.Timer).ID;

        // Save all but the current role.
        SaveRoles(ev.Mind.Session, GetTimedRoles(ev.Mind).Where(r => r != time));
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                _lastSetTime[args.Session] = _timing.RealTime;
                break;

            case SessionStatus.Disconnected:
                Save(args.Session);
                _lastSetTime.Remove(args.Session);
                break;
        }
    }

    private void OnRoundEnd(RoundRestartCleanupEvent ev)
    {
        _playTimeTracking.Save();
    }

    private void OnUnAFK(ref UnAFKEvent ev)
    {
        _lastSetTime[ev.Session] = _timing.RealTime;
    }

    private void OnAFK(ref AFKEvent ev)
    {
        Save(ev.Session);
    }

    private void FullSave()
    {
        _sawmill.Info("Running full save of role timers");

        // This is gonna have rounding if someone changes their jobs but it's only 5 minutes anyway, not like we need to track
        // per second values every time they get a new job.
        foreach (var player in Filter.GetAllPlayers())
        {
            var pSession = (IPlayerSession) player;
            if (_afk.IsAfk(pSession))
                continue;

            Save(pSession);
            _playTimeTracking.SendRoleTimers(pSession);
        }
    }

    public void Save(IPlayerSession pSession)
    {
        SaveRoles(pSession, GetTimedRoles(pSession));
    }

    private void SaveRoles(IPlayerSession pSession, IEnumerable<string> roles)
    {
        var currentTime = _timing.RealTime;
        if (!_lastSetTime.TryGetValue(pSession, out var lastSave))
            lastSave = currentTime;

        var addedTime = currentTime - lastSave;
        _sawmill.Info($"Adding {addedTime.TotalSeconds:0} seconds to {pSession} playtime");

        foreach (var role in roles)
        {
            _playTimeTracking.AddTimeToTracker(pSession.UserId, role, addedTime);
        }

        _playTimeTracking.AddTimeToOverallPlaytime(pSession.UserId, addedTime);
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

    public bool IsAllowed(NetUserId id, string role)
    {
        if (!_prototypes.TryIndex<JobPrototype>(role, out var job) ||
            job.Requirements == null ||
            !_cfg.GetCVar(CCVars.GameRoleTimers))
            return true;

        var playTimes = _playTimeTracking.GetTrackerTimes(id);

        return JobRequirements.TryRequirementsMet(id, job, playTimes, out _, _prototypes);
    }

    public HashSet<string> GetDisallowedJobs(NetUserId id)
    {
        var roles = new HashSet<string>();
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return roles;

        var playTimes = _playTimeTracking.GetTrackerTimes(id);

        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (job.Requirements != null)
            {
                foreach (var requirement in job.Requirements)
                {
                    if (JobRequirements.TryRequirementMet(id, requirement, playTimes, out _, _prototypes))
                        continue;

                    goto NoRole;
                }
            }

            roles.Add(job.ID);
            NoRole:;
        }

        return roles;
    }

    public void RemoveDisallowedJobs(NetUserId id, ref List<string> jobs)
    {
        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            return;

        var playTimes = _playTimeTracking.GetTrackerTimes(id);

        for (var i = 0; i < jobs.Count; i++)
        {
            var job = jobs[i];

            if (!_prototypes.TryIndex<JobPrototype>(job, out var jobber) ||
                jobber.Requirements == null ||
                jobber.Requirements.Count == 0)
                continue;

            foreach (var requirement in jobber.Requirements)
            {
                if (JobRequirements.TryRequirementMet(id, requirement, playTimes, out _, _prototypes))
                    continue;

                jobs.RemoveSwap(i);
                i--;
            }
        }
    }

    public void PlayerRolesChanged(IPlayerSession player)
    {
        Save(player);
    }
}

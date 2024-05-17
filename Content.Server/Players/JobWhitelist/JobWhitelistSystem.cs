using System.Collections.Immutable;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.Players.JobWhitelist;

public sealed class JobWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly JobWhitelistManager _manager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private ImmutableArray<ProtoId<JobPrototype>> _whitelistedJobs = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        SubscribeLocalEvent<StationJobsGetCandidatesEvent>(OnStationJobsGetCandidates);
        SubscribeLocalEvent<IsJobAllowedEvent>(OnIsJobAllowed);
        SubscribeLocalEvent<GetDisallowedJobsEvent>(OnGetDisallowedJobs);

        CacheJobs();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<JobPrototype>())
            CacheJobs();
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        _manager.SendJobWhitelist(ev.PlayerSession);
    }

    private void OnStationJobsGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        for (var i = ev.Jobs.Count - 1; i >= 0; i--)
        {
            var jobId = ev.Jobs[i];
            if (!_manager.IsAllowed(ev.Player, jobId))
                ev.Jobs.RemoveAt(i);
        }
    }

    private void OnIsJobAllowed(ref IsJobAllowedEvent ev)
    {
        if (!_manager.IsAllowed(ev.Player.UserId, ev.JobId))
            ev.Cancelled = true;
    }

    private void OnGetDisallowedJobs(ref GetDisallowedJobsEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        foreach (var job in _whitelistedJobs)
        {
            if (!_manager.IsAllowed(ev.Player.UserId, job))
                ev.Jobs.Add(job);
        }
    }

    private void CacheJobs()
    {
        _whitelistedJobs = [];

        var builder = ImmutableArray.CreateBuilder<ProtoId<JobPrototype>>();
        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (job.Whitelisted)
                builder.Add(job.ID);
        }

        _whitelistedJobs = builder.ToImmutable();
    }
}

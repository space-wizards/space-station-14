using System.Collections.Immutable;
using Content.Server.GameTicking.Events;
using Content.Server.Station.Events;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Players.JobWhitelist;

public sealed class JobWhitelistSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly JobWhitelistManager _manager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private ImmutableArray<ProtoId<JobPrototype>> _whitelistedJobs = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
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

    private void OnStationJobsGetCandidates(ref StationJobsGetCandidatesEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        for (var i = ev.Jobs.Count - 1; i >= 0; i--)
        {
            var jobId = ev.Jobs[i];
            if (_player.TryGetSessionById(ev.Player, out var player) &&
                !_manager.IsAllowed(player, jobId))
            {
                ev.Jobs.RemoveSwap(i);
            }
        }
    }

    private void OnIsJobAllowed(ref IsJobAllowedEvent ev)
    {
        if (!_manager.IsAllowed(ev.Player, ev.JobId))
            ev.Cancelled = true;
    }

    private void OnGetDisallowedJobs(ref GetDisallowedJobsEvent ev)
    {
        if (!_config.GetCVar(CCVars.GameRoleWhitelist))
            return;

        foreach (var job in _whitelistedJobs)
        {
            if (!_manager.IsAllowed(ev.Player, job))
                ev.Jobs.Add(job);
        }
    }

    private void CacheJobs()
    {
        var builder = ImmutableArray.CreateBuilder<ProtoId<JobPrototype>>();
        foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
        {
            if (job.Whitelisted)
                builder.Add(job.ID);
        }

        _whitelistedJobs = builder.ToImmutable();
    }
}

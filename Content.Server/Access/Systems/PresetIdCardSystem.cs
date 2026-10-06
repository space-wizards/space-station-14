using Content.Server.Access.Components;
using Content.Server.Station.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Station.Components;

namespace Content.Server.Access.Systems;

public sealed partial class PresetIdCardSystem : EntitySystem
{
    [Dependency] private IdCardSystem _card = default!;
    [Dependency] private SharedAccessSystem _access = default!;
    [Dependency] private ServerStationSystem _station = default!;

    [Dependency] private EntityQuery<StationJobsComponent> _stationJobsQuery;

    [SubscribeLocalEvent]
    private void PlayerJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        // Go over all ID cards and make sure they're correctly configured for extended access.

        var query = EntityQueryEnumerator<PresetIdCardComponent>();
        while (query.MoveNext(out var uid, out var card))
        {
            var station = _station.GetOwningStation(uid);

            // If we're not on an extended access station, the ID is already configured correctly from MapInit.
            if (station == null || !_stationJobsQuery.TryComp(station.Value, out var jobsComp) || !jobsComp.ExtendedAccess)
                continue;

            SetupIdAccess((uid, card), true);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PresetIdCardComponent> ent, ref MapInitEvent args)
    {
        // If a preset ID card is spawned on a station at setup time,
        // the station may not exist,
        // or may not yet know whether it is on extended access (players not spawned yet).
        // PlayerJobsAssigned makes sure extended access is configured correctly in that case.

        var station = _station.GetOwningStation(ent);
        var extended = false;

        // Station not guaranteed to have jobs (e.g. nukie outpost).
        if (_stationJobsQuery.TryComp(station, out var stationJobs))
            extended = stationJobs.ExtendedAccess;

        SetupIdAccess(ent, extended);
        SetupIdName(ent);
    }

    private void SetupIdName(Entity<PresetIdCardComponent> ent)
    {
        if (ent.Comp.IdName == null)
            return;

        _card.TryChangeFullName(ent, Loc.GetString(ent.Comp.IdName));
    }

    private void SetupIdAccess(Entity<PresetIdCardComponent> ent, bool extended)
    {
        if (ent.Comp.JobName == null)
            return;

        if (!ProtoMan.TryIndex(ent.Comp.JobName, out var job))
        {
            Log.Error($"Invalid job id ({ent.Comp.JobName}) for preset card");
            return;
        }

        _access.SetAccessToJob(ent, job, extended);

        _card.TryChangeJobTitle(ent, job.LocalizedName);
        _card.TryChangeJobDepartment(ent, job);

        if (ProtoMan.Resolve(job.Icon, out var jobIcon))
            _card.TryChangeJobIcon(ent, jobIcon);
    }
}

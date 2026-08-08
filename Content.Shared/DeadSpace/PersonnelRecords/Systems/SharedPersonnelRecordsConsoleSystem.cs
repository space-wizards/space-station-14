// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.PersonnelRecords.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Station;
using Content.Shared.StationRecords;

namespace Content.Shared.DeadSpace.PersonnelRecords.Systems;

/// <summary>
/// Shared base for the Personnel Records console system, mirroring
/// <c>Content.Shared.CriminalRecords.Systems.SharedCriminalRecordsConsoleSystem</c>.
///
/// Concrete BUI handling, permission checks and status-transition logic live in
/// <c>Content.Server.DeadSpace.PersonnelRecords.Systems.PersonnelRecordsConsoleSystem</c>.
/// </summary>
public abstract class SharedPersonnelRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedPersonnelRecordsSystem _personnelRecords = default!;
    [Dependency] private readonly SharedStationRecordsSystem _records = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    public override void Initialize()
    {
        base.Initialize();

        // IdentitySystem.UpdateIdentityInfo already raises IdentityChangedEvent directed at the
        // character entity (which always carries IdentityComponent) right before its own
        // criminal-records call - subscribing to that existing event gets us the same
        // "re-check icon on identity change" hook CheckNewIdentity provides for Criminal Records,
        // without touching IdentitySystem.cs at all.
        SubscribeLocalEvent<IdentityComponent, IdentityChangedEvent>(OnIdentityChanged);
    }

    private void OnIdentityChanged(Entity<IdentityComponent> ent, ref IdentityChangedEvent args)
    {
        CheckNewIdentity(ent);
    }

    /// <summary>
    /// Checks if the new identity's name has a personnel record attached to it, and gives the
    /// entity the icon that belongs to its status if so. Mirrors
    /// <c>SharedCriminalRecordsConsoleSystem.CheckNewIdentity</c> exactly.
    /// </summary>
    public void CheckNewIdentity(EntityUid uid)
    {
        var name = Identity.Name(uid, EntityManager);
        var xform = Transform(uid);

        var station = _station.GetStationInMap(xform.MapID);

        if (station != null && _records.GetRecordByName(station.Value, name) is { } id)
        {
            if (_records.TryGetRecord<PersonnelRecord>(new StationRecordKey(id, station.Value), out var record)
                && record.Status is EmploymentStatus.Demotion or EmploymentStatus.Dismissal)
            {
                _personnelRecords.SetPersonnelIcon(record.Status, uid);
                return;
            }
        }

        RemComp<PersonnelRecordComponent>(uid);
    }
}

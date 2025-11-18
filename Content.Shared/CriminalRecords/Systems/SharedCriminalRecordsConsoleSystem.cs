using Content.Shared.IdentityManagement;
using Content.Shared.Security;
using Content.Shared.Security.Components;
using Content.Shared.Station;
using Content.Shared.StationRecords;

namespace Content.Shared.CriminalRecords.Systems;

/// <summary>
/// Station records aren't predicted, just exists for access.
/// </summary>
public abstract class SharedCriminalRecordsConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedCriminalRecordsSystem _criminalRecords = default!;
    [Dependency] private readonly SharedStationRecordsSystem _records = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;

    /// <summary>
    /// Checks if the new identity's name has a criminal record attached to it, and gives the entity the icon that
    /// belongs to the status if it does.
    /// </summary>
    public void CheckNewIdentity(EntityUid uid)
    {
        var name = Identity.Name(uid, EntityManager);
        var xform = Transform(uid);

        // TODO use the entity's station? Not the station of the map that it happens to currently be on?
        var station = _station.GetStationInMap(xform.MapID);

        if (station != null && _records.GetRecordByName(station.Value, name) is { } id)
        {
            if (_records.TryGetRecord<CriminalRecord>(new StationRecordKey(id, station.Value),
                    out var record))
            {
                if (record.Status != SecurityStatus.None)
                {
                    _criminalRecords.SetCriminalIcon(name, record.Status, uid);
                    return;
                }
            }
        }
        RemComp<CriminalRecordComponent>(uid);
    }
}

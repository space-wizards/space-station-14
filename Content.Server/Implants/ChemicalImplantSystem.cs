using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.CriminalRecords;
using Content.Shared.Implants.Components;
using Content.Shared.StationRecords;
using Content.Shared.Tag;

namespace Content.Server.Implants;

public sealed class ChemicalImplantSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;

    [ValidatePrototypeId<TagPrototype>]
    public const string ChemicalImplantTag = "ChemicalImplant";

    /// <summary>
    /// Links the implant to the targets criminal record after injection is completed.
    /// </summary>
    public void LinkImplant(EntityUid implant, EntityUid implanted, SubdermalImplantComponent component)
    {
        if (!_tag.HasTag(implant, ChemicalImplantTag))
            return;

        if (component.ImplantedEntity is not { } ent)
            return;

        var name = Name(implanted);
        var xform = Transform(ent);
        var station = _station.GetStationInMap(xform.MapID);

        if (station == null || _stationRecords.GetRecordByName(station.Value, name) is not { } id)
            return;

        if (_stationRecords.TryGetRecord<CriminalRecord>(new StationRecordKey(id, station.Value),
                out var record))
        {
            var implants = new HashSet<NetEntity> { GetNetEntity(implant) };

            if(!record.Implants.TryAdd(name, implants))
                record.Implants[name].Add(GetNetEntity(implant));
        }
    }
}

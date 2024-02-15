using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared._CD.Records;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Records;

public sealed class CharacterRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: new []{ typeof(StationRecordsSystem) });
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<StationRecordsComponent>(args.Station))
        {
            Log.Error("Tried to add CharacterRecords on a station without StationRecords");
            return;
        }
        if (!HasComp<CharacterRecordsComponent>(args.Station))
            AddComp<CharacterRecordsComponent>(args.Station);

        var profile = args.Profile;
        if (profile.CDCharacterRecords == null || string.IsNullOrEmpty(args.JobId))
            return;

        var player = args.Mob;

        if (!_prototypeManager.TryIndex(args.JobId, out JobPrototype? jobPrototype))
        {
            throw new ArgumentException($"Invalid job prototype ID: {args.JobId}");
        }

        TryComp<FingerprintComponent>(player, out var fingerprintComponent);
        TryComp<DnaComponent>(player, out var dnaComponent);

        var records = new FullCharacterRecords(
            characterRecords: new CharacterRecords(profile.CDCharacterRecords),
            stationRecordsKey: FindStationRecordsKey(player),
            name: profile.Name,
            age: profile.Age,
            species: profile.Species,
            jobTitle: jobPrototype.LocalizedName,
            jobIcon: jobPrototype.Icon,
            gender: profile.Gender,
            sex: profile.Sex,
            fingerprint: fingerprintComponent?.Fingerprint,
            dna: dnaComponent?.DNA);
        AddRecord(args.Station, args.Mob, records);

        // We don't delete records after a character has joined unless an admin requests it.
    }

    private uint? FindStationRecordsKey(EntityUid uid)
    {
        if (!_inventorySystem.TryGetSlotEntity(uid, "id", out var idUid))
            return null;

        var keyStorageEntity = idUid;
        if (TryComp<PdaComponent>(idUid, out var pda) && pda.ContainedId is {} id)
        {
            keyStorageEntity = id;
        }

        if (!TryComp<StationRecordKeyStorageComponent>(keyStorageEntity, out var storage))
        {
            return null;
        }

        return storage.Key?.Id;
    }

    public bool AddRecord(EntityUid station, EntityUid player, FullCharacterRecords records, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return false;

        recordsDb.Records.Add(player, records);

        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
        return true;
    }

    public void DelEntry(EntityUid station, EntityUid player, CharacterRecordType ty, int idx, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return;

        if (!recordsDb.Records.ContainsKey(player))
            return;

        var cr = recordsDb.Records[player].CharacterRecords;

        switch (ty)
        {
            case CharacterRecordType.Employment:
                cr.EmploymentEntries.RemoveAt(idx);
                break;
            case CharacterRecordType.Medical:
                cr.MedicalEntries.RemoveAt(idx);
                break;
            case CharacterRecordType.Security:
                cr.SecurityEntries.RemoveAt(idx);
                break;
        }

        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public void ResetRecord(EntityUid station, EntityUid player, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return;

        if (!recordsDb.Records.ContainsKey(player))
            return;

        var records = CharacterRecords.DefaultRecords();
        recordsDb.Records[player].CharacterRecords = records;
        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public void DeleteAllRecords(EntityUid player)
    {
        foreach (var station in _stationSystem.GetStations())
        {
            CharacterRecordsComponent? recordsDb = null;
            if (!Resolve(station, ref recordsDb))
            {
                continue;
            }

            recordsDb.Records.Remove(player);
        }
    }

    public IDictionary<EntityUid, FullCharacterRecords> QueryRecords(EntityUid station, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return new Dictionary<EntityUid, FullCharacterRecords>();

        return recordsDb.Records;
    }
}

public sealed class CharacterRecordsModifiedEvent : EntityEventArgs
{

    public CharacterRecordsModifiedEvent()
    {
    }
}

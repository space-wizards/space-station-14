using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Server.StationRecords;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.Server._CD.Records;

public sealed class CharacterRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: [typeof(StationRecordsSystem)]);
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

        if (string.IsNullOrEmpty(args.JobId))
        {
            Log.Error($"Null JobId in CharacterRecordsSystem::OnPlayerSpawn for character {args.Profile.Name} played by {args.Player.Name}");
            return;
        }

        if (HasComp<SkipLoadingCharacterRecordsComponent>(args.Mob))
            return;

        var profile = args.Profile;
        if (profile.CDCharacterRecords == null)
        {
            Log.Error($"Null records in CharacterRecordsSystem::OnPlayerSpawn for character {args.Profile.Name} played by {args.Player.Name}.");
            return;
        }

        var player = args.Mob;

        if (!_prototype.TryIndex(args.JobId, out JobPrototype? jobPrototype))
        {
            throw new ArgumentException($"Invalid job prototype ID: {args.JobId}");
        }

        TryComp<FingerprintComponent>(player, out var fingerprintComponent);
        TryComp<DnaComponent>(player, out var dnaComponent);

        var jobTitle = jobPrototype.LocalizedName;
        var stationRecordsKey = FindStationRecordsKey(player);

        // Grab the title from the station records if they exist to support our job title system
        if (stationRecordsKey != null && _records.TryGetRecord<GeneralStationRecord>(stationRecordsKey.Value, out var stationRecords))
        {
            jobTitle = stationRecords.JobTitle;
        }

        var records = new FullCharacterRecords(
            pRecords: new PlayerProvidedCharacterRecords(profile.CDCharacterRecords),
            stationRecordsKey: stationRecordsKey?.Id,
            name: profile.Name,
            age: profile.Age,
            species: profile.Species,
            jobTitle: jobTitle,
            jobIcon: jobPrototype.Icon,
            gender: profile.Gender,
            sex: profile.Sex,
            fingerprint: fingerprintComponent?.Fingerprint,
            dna: dnaComponent?.DNA,
            owner: player);
        AddRecord(args.Station, args.Mob, records);
    }

    private StationRecordKey? FindStationRecordsKey(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "id", out var idUid))
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

        return storage.Key;
    }

    private void AddRecord(EntityUid station, EntityUid player, FullCharacterRecords records, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return;

        var key = recordsDb.CreateNewKey();
        recordsDb.Records.Add(key, records);
        var playerKey = new CharacterRecordKey { Station = station, Index = key };
        AddComp(player, new CharacterRecordKeyStorageComponent(playerKey));

        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public void DelEntry(
        EntityUid station,
        EntityUid player,
        CharacterRecordType ty,
        int idx,
        CharacterRecordsComponent? recordsDb = null,
        CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(station, ref recordsDb) || !Resolve(player, ref key))
            return;

        if (!recordsDb.Records.TryGetValue(key.Key.Index, out var value))
            return;

        var cr = value.PRecords;

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

    public void ResetRecord(
        EntityUid station,
        EntityUid player,
        CharacterRecordsComponent? recordsDb = null,
        CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(station, ref recordsDb) || !Resolve(player, ref key))
            return;

        if (!recordsDb.Records.TryGetValue(key.Key.Index, out var value))
            return;

        var records = PlayerProvidedCharacterRecords.DefaultRecords();
        if (TryComp(player, out MetaDataComponent? meta))
            value.Name = meta.EntityName;
        value.PRecords = records;
        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public void DeleteAllRecords(EntityUid player, CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(player, ref key))
            return;

        var station = key.Key.Station;
        CharacterRecordsComponent? records = null;
        if (!Resolve(station, ref records))
            return;

        records.Records.Remove(key.Key.Index);
    }

    public IDictionary<uint, FullCharacterRecords> QueryRecords(EntityUid station, CharacterRecordsComponent? recordsDb = null)
    {
        return !Resolve(station, ref recordsDb)
            ? new Dictionary<uint, FullCharacterRecords>()
            : recordsDb.Records;
    }
}

public sealed class CharacterRecordsModifiedEvent : EntityEventArgs;

using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Events;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Shared.StationRecords.Systems;

/// <summary>
///     Station records.
///
///     A station record is tied to an ID card, or anything that holds
///     a station record's key. This key will determine access to a
///     station record set's record entries, and it is imperative not
///     to lose the item that holds the key under any circumstance.
///
///     Records are mostly a roleplaying tool, but can have some
///     functionality as well (i.e., security records indicating that
///     a specific person holding an ID card with a linked key is
///     currently under warrant, showing a crew manifest with user
///     settable, custom titles).
///
///     General records are tied into this system, as most crewmembers
///     should have a general record - and most systems should probably
///     depend on this general record being created. This is subject
///     to change.
/// </summary>
public sealed partial class StationRecordsSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private StationRecordKeyStorageSystem _keyStorage = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;

    [Dependency] private EntityQuery<IdCardComponent> _idCardQuery = default!;
    [Dependency] private EntityQuery<PdaComponent> _pdaQuery = default!;
    [Dependency] private EntityQuery<StationRecordsComponent> _recordsQuery = default!;
    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _keyStorageQuery = default!;
    [Dependency] private EntityQuery<FingerprintComponent> _fingerprintQuery = default!;
    [Dependency] private EntityQuery<DnaComponent> _dnaQuery = default!;

    [SubscribeLocalEvent]
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!_recordsQuery.TryComp(args.Station, out var stationRecords))
            return;

        CreateGeneralRecord((args.Station, stationRecords), args.Mob, args.Profile, args.JobId);
    }

    [SubscribeLocalEvent]
    private void OnRename(ref EntityRenamedEvent ev)
    {
        // When a player gets renamed their card gets changed to match.
        // Unfortunately this means that an event is called for it as well, and since TryFindIdCard will succeed if the
        // given entity is a card and the card itself is the key the record will be mistakenly renamed to the card's name
        // if we don't return early.
        // We also do not include the PDA itself being renamed, as that triggers the same event (e.g. for chameleon PDAs).
        if (_idCardQuery.HasComp(ev.Uid)
            || _pdaQuery.HasComp(ev.Uid))
            return;

        if (!_idCard.TryFindIdCard(ev.Uid, out var idCard))
            return;

        if (!_keyStorageQuery.TryComp(idCard, out var keyStorage)
            || keyStorage.Key is not { } key)
            return;

        if (TryGetRecord<GeneralStationRecord>(key, out var generalRecord))
            generalRecord.Name = ev.NewName;

        Synchronize(key);
    }

    private void CreateGeneralRecord(
        Entity<StationRecordsComponent> station,
        EntityUid player,
        HumanoidCharacterProfile profile,
        string? jobId)
    {
        // TODO make PlayerSpawnCompleteEvent.JobId a ProtoId
        if (string.IsNullOrEmpty(jobId)
            || !ProtoMan.HasIndex<JobPrototype>(jobId))
            return;

        if (!_inventory.TryGetSlotEntity(player, "id", out var idUid))
            return;

        _fingerprintQuery.TryComp(player, out var fingerprintComponent);
        _dnaQuery.TryComp(player, out var dnaComponent);

        CreateGeneralRecord(
            station,
            idUid.Value,
            profile.Name,
            profile.Age,
            profile.Species,
            profile.Gender,
            jobId,
            fingerprintComponent?.Fingerprint,
            dnaComponent?.DNA,
            profile);
    }

    /// <summary>
    ///     Create a general record to store in a station's record set.
    /// </summary>
    /// <remarks>
    ///     This is tied into the record system, as any crew member's
    ///     records should generally be dependent on some generic
    ///     record with the bare minimum of information involved.
    /// </remarks>
    /// <param name="station">The entity uid of the station.</param>
    /// <param name="idUid">The entity uid of an entity's ID card. Can be null.</param>
    /// <param name="name">Name of the character.</param>
    /// <param name="species">Species of the character.</param>
    /// <param name="gender">Gender of the character.</param>
    /// <param name="jobId">
    ///     The job to initially tie this record to. This must be a valid job loaded in, otherwise
    ///     this call will cause an exception. Ensure that a general record starts out with a job
    ///     that is currently a valid job prototype.
    /// </param>
    /// <param name="mobFingerprint">Fingerprint of the character.</param>
    /// <param name="dna">DNA of the character.</param>
    ///
    /// <param name="profile">
    ///     Profile for the related player. This is so that other systems can get further information
    ///     about the player character.
    ///     Optional - other systems should anticipate this.
    /// </param>
    public void CreateGeneralRecord(
        Entity<StationRecordsComponent> station,
        EntityUid? idUid,
        string name,
        int age,
        string species,
        Gender gender,
        string jobId,
        string? mobFingerprint,
        string? dna,
        HumanoidCharacterProfile profile)
    {
        if (!ProtoMan.TryIndex<JobPrototype>(jobId, out var jobPrototype))
            throw new ArgumentException($"Invalid job prototype ID: {jobId}");

        // when adding a record that already exists use the old one
        // this happens when respawning as the same character
        if (GetRecordByName(station.AsNullable(), name) is {} id)
        {
            SetIdKey(idUid, new StationRecordKey(id, station));
            return;
        }

        var record = new GeneralStationRecord
        {
            Name = name,
            Age = age,
            JobTitle = jobPrototype.LocalizedName,
            JobIcon = jobPrototype.Icon,
            JobPrototype = jobId,
            Species = species,
            Gender = gender,
            DisplayPriority = jobPrototype.RealDisplayWeight,
            Fingerprint = mobFingerprint,
            DNA = dna
        };

        var key = AddRecordEntry(station.AsNullable(), record);
        if (!key.IsValid())
        {
            Log.Warning($"Failed to add general record entry for {name}");
            return;
        }

        SetIdKey(idUid, key);

        var ev = new GeneralRecordCreatedEvent(key, record, profile);
        RaiseLocalEvent(ref ev);

        Dirty(station);
    }

    public StationRecordKey? Convert((NetEntity, uint)? input)
    {
        return input == null ? null : Convert(input.Value);
    }

    public (NetEntity, uint)? Convert(StationRecordKey? input)
    {
        return input == null ? null : Convert(input.Value);
    }

    public StationRecordKey Convert((NetEntity, uint) input)
    {
        return new StationRecordKey(input.Item2, GetEntity(input.Item1));
    }
    public (NetEntity, uint) Convert(StationRecordKey input)
    {
        return (GetNetEntity(input.OriginStation), input.Id);
    }

    public List<(NetEntity, uint)> Convert(ICollection<StationRecordKey> input)
    {
        var result = new List<(NetEntity, uint)>(input.Count);
        foreach (var entry in input)
        {
            result.Add(Convert(entry));
        }
        return result;
    }

    public List<StationRecordKey> Convert(ICollection<(NetEntity, uint)> input)
    {
        var result = new List<StationRecordKey>(input.Count);
        foreach (var entry in input)
        {
            result.Add(Convert(entry));
        }
        return result;
    }
}

using System.Diagnostics.CodeAnalysis;
using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

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
public sealed class StationRecordsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StationRecordKeyStorageSystem _keyStorageSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialize);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnStationInitialize(StationInitializedEvent args)
    {
        AddComp<StationRecordsComponent>(args.Station);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        CreateGeneralRecord(args.Station, args.Mob, args.Profile, args.JobId);
    }

    private void CreateGeneralRecord(EntityUid station, EntityUid player, HumanoidCharacterProfile profile,
        string? jobId, StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records)
            || String.IsNullOrEmpty(jobId)
            || !_prototypeManager.HasIndex<JobPrototype>(jobId))
        {
            return;
        }

        if (!_inventorySystem.TryGetSlotEntity(player, "id", out var idUid))
        {
            return;
        }

        CreateGeneralRecord(station, idUid.Value, profile.Name, profile.Age, profile.Species, profile.Gender, jobId, profile, records);
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
    /// <param name="profile">
    ///     Profile for the related player. This is so that other systems can get further information
    ///     about the player character.
    ///     Optional - other systems should anticipate this.
    /// </param>
    /// <param name="records">Station records component.</param>
    public void CreateGeneralRecord(EntityUid station, EntityUid? idUid, string name, int age, string species, Gender gender, string jobId, HumanoidCharacterProfile? profile = null,
        StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records))
        {
            return;
        }

        if (!_prototypeManager.TryIndex(jobId, out JobPrototype? jobPrototype))
        {
            throw new ArgumentException($"Invalid job prototype ID: {jobId}");
        }

        var record = new GeneralStationRecord()
        {
            Name = name,
            Age = age,
            JobTitle = jobPrototype.LocalizedName,
            JobIcon = jobPrototype.Icon,
            JobPrototype = jobId,
            Species = species,
            Gender = gender,
            DisplayPriority = jobPrototype.Weight
        };

        var key = AddRecord(station, records);
        AddRecordEntry(key, record, records);

        if (idUid != null)
        {
            var keyStorageEntity = idUid;
            if (TryComp(idUid, out PDAComponent? pdaComponent) && pdaComponent.ContainedID != null)
            {
                keyStorageEntity = pdaComponent.IdSlot.Item;
            }

            if (keyStorageEntity != null)
            {
                _keyStorageSystem.AssignKey(keyStorageEntity.Value, key);
            }
        }

        RaiseLocalEvent(new AfterGeneralRecordCreatedEvent(key, record, profile));
    }

    /// <summary>
    ///     Removes a record from this station.
    /// </summary>
    /// <param name="station">Station to remove the record from.</param>
    /// <param name="key">The key to remove.</param>
    /// <param name="records">Station records component.</param>
    /// <returns>True if the record was removed, false otherwise.</returns>
    public bool RemoveRecord(EntityUid station, StationRecordKey key, StationRecordsComponent? records = null)
    {
        if (station != key.OriginStation || !Resolve(station, ref records))
        {
            return false;
        }

        RaiseLocalEvent(new RecordRemovedEvent(key));

        return records.Records.RemoveAllRecords(key);
    }

    /// <summary>
    ///     Try to get a record from this station's record entries,
    ///     from the provided station record key. Will always return
    ///     null if the key does not match the station.
    /// </summary>
    /// <param name="station">Station to get the record from.</param>
    /// <param name="key">Key to try and index from the record set.</param>
    /// <param name="entry">The resulting entry.</param>
    /// <param name="records">Station record component.</param>
    /// <typeparam name="T">Type to get from the record set.</typeparam>
    /// <returns>True if the record was obtained, false otherwise.</returns>
    public bool TryGetRecord<T>(EntityUid station, StationRecordKey key, [NotNullWhen(true)] out T? entry, StationRecordsComponent? records = null)
    {
        entry = default;

        if (key.OriginStation != station || !Resolve(station, ref records))
        {
            return false;
        }

        return records.Records.TryGetRecordEntry(key, out entry);
    }

    /// <summary>
    ///     Gets all records of a specific type from a station.
    /// </summary>
    /// <param name="station">The station to get the records from.</param>
    /// <param name="records">Station records component.</param>
    /// <typeparam name="T">Type of record to fetch</typeparam>
    /// <returns>Enumerable of pairs with a station record key, and the entry in question of type T.</returns>
    public IEnumerable<(StationRecordKey, T)> GetRecordsOfType<T>(EntityUid station, StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records))
        {
            return new (StationRecordKey, T)[]{};
        }

        return records.Records.GetRecordsOfType<T>();
    }

    /// <summary>
    ///     Adds a record to a station's record set.
    /// </summary>
    /// <param name="station">The station to add a record to.</param>
    /// <param name="records">Station records component.</param>
    /// <returns>
    ///     A station record key, which can be used to add and get records.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Occurs when the entity given does not have a station records component.
    /// </exception>
    public StationRecordKey AddRecord(EntityUid station, StationRecordsComponent? records)
    {
        if (!Resolve(station, ref records))
        {
            throw new ArgumentException($"Could not retrieve a {nameof(StationRecordsComponent)} from entity {station}");
        }

        return records.Records.AddRecord(station);
    }

    /// <summary>
    ///     Adds a record entry to a station's record set.
    /// </summary>
    /// <param name="key">The key to add the record to.</param>
    /// <param name="record">The record to add.</param>
    /// <param name="records">Station records component.</param>
    /// <typeparam name="T">The type of record to add.</typeparam>
    public void AddRecordEntry<T>(StationRecordKey key, T record,
        StationRecordsComponent? records = null)
    {
        if (!Resolve(key.OriginStation, ref records))
        {
            return;
        }

        records.Records.AddRecordEntry(key, record);
    }

    /// <summary>
    ///     Synchronizes a station's records with any systems that need it.
    /// </summary>
    /// <param name="station">The station to synchronize any recently accessed records with..</param>
    /// <param name="records">Station records component.</param>
    public void Synchronize(EntityUid station, StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records))
        {
            return;
        }

        foreach (var key in records.Records.GetRecentlyAccessed())
        {
            RaiseLocalEvent(new RecordModifiedEvent(key));
        }

        records.Records.ClearRecentlyAccessed();
    }
}

/// <summary>
///     Event raised after the player's general profile is created.
///     Systems that modify records on a station would have more use
///     listening to this event, as it contains the character's record key.
///     Also stores the general record reference, to save some time.
/// </summary>
public sealed class AfterGeneralRecordCreatedEvent : EntityEventArgs
{
    public StationRecordKey Key { get; }
    public GeneralStationRecord Record { get; }
    /// <summary>
    /// Profile for the related player. This is so that other systems can get further information
    ///     about the player character.
    ///     Optional - other systems should anticipate this.
    /// </summary>
    public HumanoidCharacterProfile? Profile { get; }

    public AfterGeneralRecordCreatedEvent(StationRecordKey key, GeneralStationRecord record, HumanoidCharacterProfile? profile)
    {
        Key = key;
        Record = record;
        Profile = profile;
    }
}

/// <summary>
///     Event raised after a record is removed. Only the key is given
///     when the record is removed, so that any relevant systems/components
///     that store record keys can then remove the key from their internal
///     fields.
/// </summary>
public sealed class RecordRemovedEvent : EntityEventArgs
{
    public StationRecordKey Key { get; }

    public RecordRemovedEvent(StationRecordKey key)
    {
        Key = key;
    }
}

/// <summary>
///     Event raised after a record is modified. This is to
///     inform other systems that records stored in this key
///     may have changed.
/// </summary>
public sealed class RecordModifiedEvent : EntityEventArgs
{
    public StationRecordKey Key { get; }

    public RecordModifiedEvent(StationRecordKey key)
    {
        Key = key;
    }
}

using Content.Server.Access.Systems;
using Content.Server.GameTicking;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Shared.Access.Components;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

public sealed class StationRecordsSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IdCardSystem _idCardSystem = default!;
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

    /// <summary>
    ///     Creates a general record for a player. Usually requires that they have an ID.
    /// </summary>
    /// <param name="station"></param>
    /// <param name="player"></param>
    /// <param name="profile"></param>
    /// <param name="jobId"></param>
    /// <param name="records"></param>
    private void CreateGeneralRecord(EntityUid station, EntityUid player, HumanoidCharacterProfile profile, string? jobId,
        StationRecordsComponent? records = null)
    {
        if (!Resolve(station, ref records)
            || String.IsNullOrEmpty(jobId)
            || !_prototypeManager.TryIndex(jobId, out JobPrototype? jobPrototype))
        {
            return;
        }

        if (!_inventorySystem.TryGetSlotEntity(player, "id", out var idUid))
        {
            return;
        }

        var record = new GeneralStationRecord()
        {
            Name = profile.Name,
            JobTitle = jobPrototype.Name,
            JobId = jobId,
            Species = profile.Species,
            Gender = profile.Gender
        };

        record.Departments.AddRange(jobPrototype.Departments);

        (var key, var entry) = records.Records.AddRecord(station);
        entry.Entries.Add(typeof(GeneralStationRecord), record);

        EntityUid? keyStorageEntity = idUid;
        if (TryComp(idUid, out PDAComponent? pdaComponent) && pdaComponent.ContainedID != null)
        {
            keyStorageEntity = pdaComponent.IdSlot.Item;
        }

        if (keyStorageEntity != null && TryComp(keyStorageEntity, out StationRecordKeyStorageComponent? keyStorage))
        {
            keyStorage.Key = key;
        }

        RaiseLocalEvent(new AfterGeneralRecordCreated(key, record, player, profile));
    }
}

/// <summary>
///     Event raised after the player's general profile is created.
///     Systems that modify records on a station would have more use
///     listening to this event, as it contains the character's record key.
///     Also stores the general record reference, to save some time.
/// </summary>
public sealed class AfterGeneralRecordCreated : EntityEventArgs
{
    public StationRecordKey Key { get; }
    public GeneralStationRecord Record { get; }
    public EntityUid Mob { get; }
    public HumanoidCharacterProfile Profile { get; }

    public AfterGeneralRecordCreated(StationRecordKey key, GeneralStationRecord record, EntityUid mob, HumanoidCharacterProfile profile)
    {
        Key = key;
        Record = record;
        Mob = mob;
        Profile = profile;
    }
}

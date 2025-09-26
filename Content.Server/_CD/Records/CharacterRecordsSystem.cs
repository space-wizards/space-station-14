using System;
using System.Collections.Generic;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Content.Shared._CD.Records;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Localization;
using Content.Shared.Preferences; // Loc.TryGetString

namespace Content.Server._CD.Records;

/// <summary>
/// Keeps the runtime record database for players on a station and exposes helpers to mutate it.
/// </summary>
public sealed class CharacterRecordsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;

    /// <summary>
    /// Used when no usable species information is available.
    /// Prefer a stable constant over an empty string to keep UIs predictable.
    /// </summary>
    private const string UnknownSpeciesDisplay = "Unknown";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn, after: new[] { typeof(StationRecordsSystem) });
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<StationRecordsComponent>(args.Station))
        {
            Log.Error("Tried to add CharacterRecords on a station without StationRecords.");
            return;
        }

        if (!HasComp<CharacterRecordsComponent>(args.Station))
            AddComp<CharacterRecordsComponent>(args.Station);

        if (args.Profile is null)
        {
            Log.Error($"Null Profile in CharacterRecordsSystem::OnPlayerSpawn for player {args.Player?.Name ?? "<unknown>"}.");
            return;
        }

        if (string.IsNullOrEmpty(args.JobId))
        {
            Log.Error($"Null/Empty JobId in CharacterRecordsSystem::OnPlayerSpawn for character {args.Profile.Name} played by {args.Player.Name}.");
            return;
        }

        if (HasComp<SkipLoadingCharacterRecordsComponent>(args.Mob))
            return;

        var profile = args.Profile;

        // Use the player's saved records when available; otherwise seed with the default template.
        var profileRecords = profile.CDCharacterRecords ?? PlayerProvidedCharacterRecords.DefaultRecords();

        if (!CharacterRecordSizeHelper.TryCalculateMetrics(profile, _prototype, out var derivedHeight, out var derivedWeight))
        {
            // Fall back to whatever the profile already stored if we cannot resolve the species sizing data.
            Log.Warning($"Failed to resolve species sizing while constructing records for {profile.Name}. Using stored values.");
        }
        else
        {
            profileRecords = profileRecords.WithHeight(derivedHeight).WithWeight(derivedWeight);
        }
        if (!_prototype.TryIndex(args.JobId, out JobPrototype? jobPrototype))
        {
            Log.Error($"Invalid job prototype ID '{args.JobId}' while creating records for {profile.Name}.");
            return;
        }

        var player = args.Mob;

        TryComp<FingerprintComponent>(player, out var fingerprintComponent);
        TryComp<DnaComponent>(player, out var dnaComponent);

        var jobTitle = jobPrototype.LocalizedName;

        // Cross-reference the station data so we can keep the runtime record in sync.
        var stationRecordsKey = FindStationRecordsKey(player);

        if (stationRecordsKey != null && _records.TryGetRecord<GeneralStationRecord>(stationRecordsKey.Value, out var stationRecord))
        {
            // Prefer the live station record title in case the job changed after spawning.
            jobTitle = stationRecord.JobTitle;
        }

        // Resolve a readable species display name:
        // - If a custom species name is set and differs from the base, show "Custom (Base)".
        // - Otherwise show only the base display (localized if possible).
        var speciesName = GetReadableSpeciesName(profile);

        // Build the composite record that consoles consume, mixing profile data with live round metadata.
        var records = new FullCharacterRecords(
            pRecords: new PlayerProvidedCharacterRecords(profileRecords),
            stationRecordsKey: stationRecordsKey?.Id,
            name: profile.Name,
            age: profile.Age,
            species: speciesName,
            jobTitle: jobTitle,
            jobIcon: jobPrototype.Icon,
            gender: profile.Gender,
            sex: profile.Sex,
            fingerprint: fingerprintComponent?.Fingerprint,
            dna: dnaComponent?.DNA,
            owner: player);

        AddRecord(args.Station, player, records);
    }

    /// <summary>
    /// Resolves a localized, human-readable base species display name.
    /// Fallback order:
    /// - Localization of prototype display name (proto.Name as loc key)
    /// - Raw prototype display name (proto.Name)
    /// - Raw prototype ID (speciesId)
    /// - Constant "Unknown" if everything else is empty
    /// </summary>
    private string ResolveBaseSpeciesDisplayName(string? speciesId)
    {
        if (string.IsNullOrWhiteSpace(speciesId))
            return UnknownSpeciesDisplay;

        if (_prototype.TryIndex<SpeciesPrototype>(speciesId, out var proto))
        {
            // proto.Name is typically a localization key
            if (Loc.TryGetString(proto.Name, out var localized) && !string.IsNullOrWhiteSpace(localized))
                return localized;

            if (!string.IsNullOrWhiteSpace(proto.Name))
                return proto.Name;
        }

        // Fallback to the raw prototype ID, or "Unknown" if even that is unusable.
        return !string.IsNullOrWhiteSpace(speciesId) ? speciesId : UnknownSpeciesDisplay;
    }

    /// <summary>
    /// Returns "Custom (Base)" when a differing custom name exists; otherwise only the base display.
    /// Robust against null/whitespace and avoids duplicates like "X (X)".
    /// </summary>
    private string GetReadableSpeciesName(HumanoidCharacterProfile profile)
    {
        var baseDisplay = ResolveBaseSpeciesDisplayName(profile?.Species);
        var custom = profile?.CustomSpecieName;

        if (!string.IsNullOrWhiteSpace(custom))
        {
            // Avoid duplication (case-insensitive compare); trim to avoid cosmetic whitespace issues.
            var customTrimmed = custom.Trim();
            if (!customTrimmed.Equals(baseDisplay, StringComparison.OrdinalIgnoreCase))
                return $"{customTrimmed} ({baseDisplay})";
        }

        return baseDisplay;
    }

    private StationRecordKey? FindStationRecordsKey(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, "id", out var idUid))
            return null;

        var keyStorageEntity = idUid;

        // Many ID cards live inside PDAs; follow the chain to the actual card that stores the key.
        if (TryComp<PdaComponent>(idUid, out var pda) && pda.ContainedId is { } id)
            keyStorageEntity = id;

        if (!TryComp<StationRecordKeyStorageComponent>(keyStorageEntity, out var storage))
            return null;

        return storage.Key;
    }

    private void AddRecord(EntityUid station, EntityUid player, FullCharacterRecords records, CharacterRecordsComponent? recordsDb = null)
    {
        if (!Resolve(station, ref recordsDb))
            return;

        // Persist the record and remember which entry belongs to the player for later lookups.
        var key = recordsDb.CreateNewKey();
        recordsDb.Records.Add(key, records);
        var playerKey = new CharacterRecordKey { Station = station, Index = key };
        AddComp(player, new CharacterRecordKeyStorageComponent(playerKey));

        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public void DelEntry(
        EntityUid station,
        EntityUid player,
        CharacterRecordType type,
        int index,
        CharacterRecordsComponent? recordsDb = null,
        CharacterRecordKeyStorageComponent? key = null)
    {
        if (!Resolve(station, ref recordsDb) || !Resolve(player, ref key))
            return;

        if (!recordsDb.Records.TryGetValue(key.Key.Index, out var value))
            return;

        var playerRecords = value.PRecords;

        // Entries are segmented by category; drop the requested item if it exists.
        switch (type)
        {
            case CharacterRecordType.Employment:
                if (index >= 0 && index < playerRecords.EmploymentEntries.Count)
                    playerRecords.EmploymentEntries.RemoveAt(index);
                break;
            case CharacterRecordType.Medical:
                if (index >= 0 && index < playerRecords.MedicalEntries.Count)
                    playerRecords.MedicalEntries.RemoveAt(index);
                break;
            case CharacterRecordType.Security:
                if (index >= 0 && index < playerRecords.SecurityEntries.Count)
                    playerRecords.SecurityEntries.RemoveAt(index);
                break;
            case CharacterRecordType.Admin:
                if (index >= 0 && index < playerRecords.AdminEntries.Count)
                    playerRecords.AdminEntries.RemoveAt(index);
                break;
            default:
                // Unknown type: no-op by design (defensive behavior).
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

        // Replace the player-authored information with a clean template.
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

        // Remove the entire record entry for this player, e.g., when the entity is deleted mid-round.
        records.Records.Remove(key.Key.Index);
        RaiseLocalEvent(station, new CharacterRecordsModifiedEvent());
    }

    public IDictionary<uint, FullCharacterRecords> QueryRecords(EntityUid station, CharacterRecordsComponent? recordsDb = null)
    {
        // Provide a safe empty map when the station lacks runtime record state.
        return !Resolve(station, ref recordsDb)
            ? new Dictionary<uint, FullCharacterRecords>()
            : recordsDb.Records;
    }
}

public sealed class CharacterRecordsModifiedEvent : EntityEventArgs;


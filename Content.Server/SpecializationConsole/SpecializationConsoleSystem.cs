using Content.Server.Access.Systems;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.CrewManifest;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.Database;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.SpecializationConsole;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SpecializationConsole;

public sealed class SpecializationConsoleSystem : SharedSpecializationConsoleSystem
{
    [Dependency] private readonly CrewManifestSystem _crewManifest = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly StationRecordsSystem _record = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
        SubscribeLocalEvent<SpecializationConsoleComponent, SpecializationChangedMessage>(OnSpecializationChanged);
    }

    private void OnSpecializationChanged(EntityUid uid, SpecializationConsoleComponent comp, SpecializationChangedMessage args)
    {
        if (!TryComp<SpecializationConsoleComponent>(uid, out var card) ||
            card.TargetIdSlot.Item is not { Valid: true } targetId)
            return;

        _idCard.TryChangeJobSpec(targetId, args.Specialization);
        UpdateStationRecord(uid, targetId, args.Specialization);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        if (!_record.TryGetRecord<GeneralStationRecord>(ev.Key, out var record))
            return;
        record.Profile = ev.Profile;
        _record.Synchronize(ev.Key);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, string? newSpecTitle)
    {
        if (!TryComp<StationRecordInfoStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        record.JobSpecialization = newSpecTitle ?? string.Empty;
        _record.Synchronize(key);
    }

}

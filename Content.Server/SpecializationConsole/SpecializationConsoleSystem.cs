using Content.Server.Access.Systems;
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
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        // SubscribeLocalEvent<SpecializationConsoleComponent, SpecializationChangedMessage>(OnSpecializationChanged);
        SubscribeLocalEvent<SpecializationConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<SpecializationConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnGeneralRecordCreated);
    }

    private void OnGeneralRecordCreated(AfterGeneralRecordCreatedEvent ev)
    {
        if (!_record.TryGetRecord<GeneralStationRecord>(ev.Key, out var record))
            return;
        record.Profile = ev.Profile;
        _record.Synchronize(ev.Key);
    }

    private void UpdateUserInterface(EntityUid uid,
        SpecializationConsoleComponent component,
        EntityEventArgs args)
    {
        HumanoidCharacterProfile? profile = null;
        ProtoId<JobPrototype>? job = null;
        SpecializationConsoleBoundInterfaceState newState;

        // this could be prettier
        if (component.TargetIdSlot.Item is not { Valid: true } targetId)
        {
            newState = new SpecializationConsoleBoundInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                component.TargetIdSlot.HasItem,
                null,
                null,
                null,
                null,
                null);
        }
        else
        {
            var targetIdComponent = Comp<IdCardComponent>(targetId);

            if (TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStor) &&
                keyStor.Key is {} key &&
                _record.TryGetRecord<GeneralStationRecord>(key, out var rec))
            {
                profile = rec.Profile;
                job = rec.JobPrototype;
            }

            newState = new SpecializationConsoleBoundInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                component.TargetIdSlot.HasItem,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                targetIdComponent.JobSpecTitle,
                profile,
                job);

        }
        _userInterfaceSystem.SetUiState(uid, SpecializationConsoleWindowUiKey.Key, newState);
    }

    private void UpdateStationRecord(EntityUid uid, EntityUid targetId, ProtoId<AccessLevelPrototype> newJobTitle, JobPrototype? newJobProto)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_record.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        record.JobTitle = newJobTitle;

        if (newJobProto != null)
        {
            record.JobPrototype = newJobProto.ID;
            record.JobIcon = newJobProto.Icon;
        }

        _record.Synchronize(key);
    }

    private bool PrivilegedIdIsAuthorized(EntityUid uid, IdCardConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (!TryComp<AccessReaderComponent>(uid, out var reader))
            return true;

        var privilegedId = component.PrivilegedIdSlot.Item;
        return privilegedId != null && _accessReader.IsAllowed(privilegedId.Value, uid, reader);
    }

    // private void OnSpecializationChanged(EntityUid uid,
    //     SpecializationConsoleComponent component,
    //     SpecializationChangedMessage args)
    // {
    //     if (!TryComp<IdCardConsoleComponent>(uid, out var card))
    //         return;
    //
    //     if (card.TargetIdSlot.Item is not { Valid: true } targetId || !PrivilegedIdIsAuthorized(uid, card))
    //         return;
    //     if (!TryComp<IdCardComponent>(targetId, out var idCard))
    //         return;
    //
    //     _idCard.TryChangeJobSpec(targetId, idCard.LocalizedJobTitle + "(" + args.SpecName + ")");
    //
    //     if (idCard.JobPrototype != null
    //         && _prototype.TryIndex<JobPrototype>(idCard.JobPrototype, out var job))
    //         UpdateStationRecord(uid, targetId, args.SpecName, job);
    // }


    private void UpdateUiState(EntityUid uid, SpecializationConsoleComponent component)
    {
        // var uiState = new SpecializationConsoleBoundInterfaceState();
        // _userInterfaceSystem.SetUiState(uid, SpecializationConsoleWindowUiKey.Key, uiState);
    }
}

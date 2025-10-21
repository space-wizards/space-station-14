using Content.Client.Access;
using Content.Client.Roles;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.CCVar;
using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Content.Shared.SpecializationConsole;
using Content.Shared.StationRecords;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Client.SpecializationConsole;

public sealed class SpecializationConsoleSystem : SharedSpecializationConsoleSystem
{
    [Dependency] private UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedStationRecordsSystem _records = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpecializationConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<SpecializationConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<SpecializationConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
    }

    private void UpdateUserInterface(EntityUid uid,
        SpecializationConsoleComponent component,
        EntityEventArgs args)
    {
        SpecializationConsoleBoundInterfaceState newState;
        if (component.TargetIdSlot.Item is { Valid: true } targetId)
        {
            var targetIdComponent = Comp<IdCardComponent>(targetId);

            if (!TryComp<StationRecordInfoStorageComponent>(targetId, out var keyStorage))
                return;
            var stationRecord = keyStorage.Record;

            newState = new SpecializationConsoleBoundInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                component.TargetIdSlot.HasItem,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                targetIdComponent.JobSpecializationTitle,
                stationRecord?.Profile,
                stationRecord?.JobPrototype);
        }
        else
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
        _userInterfaceSystem.SetUiState(uid, SpecializationConsoleWindowUiKey.Key, newState);
    }
}

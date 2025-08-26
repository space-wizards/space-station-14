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

        // SubscribeLocalEvent<SpecializationConsoleComponent, SpecializationChangedMessage>(OnSpecializationChanged);
        SubscribeLocalEvent<SpecializationConsoleComponent, EntInsertedIntoContainerMessage>(UpdateUserInterface);
        SubscribeLocalEvent<SpecializationConsoleComponent, EntRemovedFromContainerMessage>(UpdateUserInterface);
    }

    private void UpdateUserInterface(EntityUid uid,
        SpecializationConsoleComponent component,
        EntityEventArgs args)
    {
        SpecializationConsoleBoundInterfaceState newState;
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

            if (!TryComp<StationRecordKeyStorageComponent>(targetId, out var keyStorage))
                return;
            var stationRecord = keyStorage.CachedRecord;


            newState = new SpecializationConsoleBoundInterfaceState(
                component.PrivilegedIdSlot.HasItem,
                component.TargetIdSlot.HasItem,
                targetIdComponent.FullName,
                targetIdComponent.LocalizedJobTitle,
                targetIdComponent.JobSpecTitle,
                stationRecord?.Profile,
                stationRecord?.JobPrototype);
        }

        _userInterfaceSystem.SetUiState(uid, SpecializationConsoleWindowUiKey.Key, newState);

    }
}

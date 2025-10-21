using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.SpecializationConsole
{
    [UsedImplicitly]
    public abstract class SharedSpecializationConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly SharedStationRecordsSystem _records = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpecializationConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SpecializationConsoleComponent, ComponentRemove>(OnComponentRemove);
            // SubscribeLocalEvent<SpecializationConsoleComponent, NewEmployeeDataEvent>(OnNewEmployeeDataEvent);
        }

        // private void OnNewEmployeeDataEvent(EntityUid uid,
        //     SpecializationConsoleComponent component,
        //     NewEmployeeDataEvent args)
        // {
        //     if (!TryComp<StationRecordKeyStorageComponent>(uid, out var keyStorage))
        //         return;
        //     _records.Convert(keyStorage.Key);
        // }

        private void OnComponentInit(EntityUid uid, SpecializationConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SpecializationConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
            _itemSlotsSystem.AddItemSlot(uid, SpecializationConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, SpecializationConsoleComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
            _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
        }
    }
}

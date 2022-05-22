using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    [UsedImplicitly]
    public abstract class SharedIdCardConsoleSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;

        public const string Sawmill = "idconsole";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<SharedIdCardConsoleComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, SharedIdCardConsoleComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not IdCardConsoleComponentState state) return;
            component.AccessLevels = state.AccessLevels;
        }

        private void OnGetState(EntityUid uid, SharedIdCardConsoleComponent component, ref ComponentGetState args)
        {
            args.State = new IdCardConsoleComponentState(component.AccessLevels);
        }

        private void OnComponentInit(EntityUid uid, SharedIdCardConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, SharedIdCardConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
            _itemSlotsSystem.AddItemSlot(uid, SharedIdCardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, SharedIdCardConsoleComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
            _itemSlotsSystem.RemoveItemSlot(uid, component.TargetIdSlot);
        }

        [Serializable, NetSerializable]
        private sealed class IdCardConsoleComponentState : ComponentState
        {
            public List<string> AccessLevels;

            public IdCardConsoleComponentState(List<string> accessLevels)
            {
                AccessLevels = accessLevels;
            }
        }
    }
}

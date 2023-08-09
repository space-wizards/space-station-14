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
        [Dependency] private readonly ILogManager _log = default!;

        public const string Sawmill = "idconsole";
        protected ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _log.GetSawmill(Sawmill);

            SubscribeLocalEvent<IdCardConsoleComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<IdCardConsoleComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<IdCardConsoleComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<IdCardConsoleComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, IdCardConsoleComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not IdCardConsoleComponentState state) return;
            component.AccessLevels = state.AccessLevels;
        }

        private void OnGetState(EntityUid uid, IdCardConsoleComponent component, ref ComponentGetState args)
        {
            args.State = new IdCardConsoleComponentState(component.AccessLevels);
        }

        private void OnComponentInit(EntityUid uid, IdCardConsoleComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, IdCardConsoleComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
            _itemSlotsSystem.AddItemSlot(uid, IdCardConsoleComponent.TargetIdCardSlotId, component.TargetIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, IdCardConsoleComponent component, ComponentRemove args)
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

using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

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

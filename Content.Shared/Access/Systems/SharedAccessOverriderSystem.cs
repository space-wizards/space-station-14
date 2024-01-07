using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.Access.Systems
{
    [UsedImplicitly]
    public abstract partial class SharedAccessOverriderSystem : EntitySystem
    {
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly ILogManager _log = default!;

        public const string Sawmill = "accessoverrider";
        protected ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            _sawmill = _log.GetSawmill(Sawmill);

            SubscribeLocalEvent<AccessOverriderComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<AccessOverriderComponent, ComponentRemove>(OnComponentRemove);
        }

        private void OnComponentInit(EntityUid uid, AccessOverriderComponent component, ComponentInit args)
        {
            _itemSlotsSystem.AddItemSlot(uid, AccessOverriderComponent.PrivilegedIdCardSlotId, component.PrivilegedIdSlot);
        }

        private void OnComponentRemove(EntityUid uid, AccessOverriderComponent component, ComponentRemove args)
        {
            _itemSlotsSystem.RemoveItemSlot(uid, component.PrivilegedIdSlot);
        }

        [Serializable, NetSerializable]
        public sealed partial class AccessOverriderDoAfterEvent : DoAfterEvent
        {
            public AccessOverriderDoAfterEvent()
            {
            }

            public override DoAfterEvent Clone() => this;
        }
    }
}

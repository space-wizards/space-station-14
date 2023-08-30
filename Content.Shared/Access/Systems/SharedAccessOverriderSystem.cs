using Content.Shared.Access.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
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
            SubscribeLocalEvent<AccessOverriderComponent, ComponentGetState>(OnGetState);
            SubscribeLocalEvent<AccessOverriderComponent, ComponentHandleState>(OnHandleState);
        }

        private void OnHandleState(EntityUid uid, AccessOverriderComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not AccessOverriderComponentState state) return;
            component.AccessLevels = state.AccessLevels;
        }

        private void OnGetState(EntityUid uid, AccessOverriderComponent component, ref ComponentGetState args)
        {
            args.State = new AccessOverriderComponentState(component.AccessLevels);
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
        private sealed class AccessOverriderComponentState : ComponentState
        {
            public List<string> AccessLevels;

            public AccessOverriderComponentState(List<string> accessLevels)
            {
                AccessLevels = accessLevels;
            }
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

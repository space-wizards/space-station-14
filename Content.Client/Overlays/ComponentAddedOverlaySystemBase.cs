using Content.Shared.Clothing.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.Overlays
{
    public abstract class ComponentAddedOverlaySystemBase<T> : EntitySystem where T : IComponent
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly InventorySystem _invSystem = default!;

        protected bool IsActive = false;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<T, ComponentInit>(OnInit);
            SubscribeLocalEvent<T, ComponentRemove>(OnRemove);

            SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);

            SubscribeLocalEvent<T, GotEquippedEvent>(OnCompEquip);
            SubscribeLocalEvent<T, GotUnequippedEvent>(OnCompUnequip);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        }

        public void ApplyOverlay(T component)
        {
            IsActive = true;
            OnApplyOverlay(component);
        }

        public void RemoveOverlay()
        {
            IsActive = false;
            OnRemoveOverlay();
        }

        protected virtual void OnApplyOverlay(T component) { }

        protected virtual void OnRemoveOverlay() { }

        private void OnInit(EntityUid uid, T component, ComponentInit args)
        {
            RefreshOverlay(uid);
        }

        private void OnRemove(EntityUid uid, T component, ComponentRemove args)
        {
            RefreshOverlay(uid);
        }

        private void OnPlayerAttached(PlayerAttachedEvent args)
        {
            RefreshOverlay(args.Entity);
        }

        private void OnPlayerDetached(PlayerDetachedEvent args)
        {
            RefreshOverlay(args.Entity);
        }

        private void OnCompEquip(EntityUid uid, T component, GotEquippedEvent args)
        {
            RefreshOverlay(args.Equipee);
        }

        private void OnCompUnequip(EntityUid uid, T component, GotUnequippedEvent args)
        {
            RefreshOverlay(args.Equipee);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }

        private void RefreshOverlay(EntityUid uid)
        {
            RemoveOverlay();

            if (uid != _player.LocalPlayer?.ControlledEntity)
            {
                return;
            }

            if (TryComp<T>(uid, out var component))
            {
                ApplyOverlay(component);
            }

            if (!(TryComp(uid, out InventoryComponent? inventoryComponent)
                && _invSystem.TryGetSlots(uid, out var slotDefinitions, inventoryComponent)))
            {
                return;
            }

            foreach (var slot in slotDefinitions)
            {
                if (!(_invSystem.TryGetSlotEntity(uid, slot.Name, out var itemUid)
                    && TryComp(itemUid.Value, out component)))
                {
                    continue;
                }

                if (!TryComp<ClothingComponent>(itemUid, out var clothing))
                {
                    continue;
                }

                if (!clothing.Slots.HasFlag(slot.SlotFlags))
                {
                    continue;
                }

                ApplyOverlay(component);
            }
        }
    }
}

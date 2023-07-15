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
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private InventorySystem _invSystem = default!;

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

            _invSystem = _entityManager.System<InventorySystem>();
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
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                ApplyOverlay(component);
            }
        }

        private void OnRemove(EntityUid uid, T component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                RemoveOverlay();
            }
        }

        private void OnPlayerAttached(PlayerAttachedEvent args)
        {
            if (TryComp<T>(args.Entity, out var component))
                ApplyOverlay(component);

            if (TryComp(args.Entity, out InventoryComponent? inventoryComponent)
                && _invSystem.TryGetSlots(args.Entity, out var slotDefinitions, inventoryComponent))
            {
                foreach (var slot in slotDefinitions)
                {
                    if (_invSystem.TryGetSlotEntity(args.Entity, slot.Name, out var itemUid)
                        && TryComp(itemUid.Value, out component))
                    {
                        OnCompEquip(itemUid.Value, component, new GotEquippedEvent(args.Entity, itemUid.Value, slot));
                    }
                }
            }
        }

        private void OnPlayerDetached(PlayerDetachedEvent args)
        {
            RemoveOverlay();
        }

        private void OnCompEquip(EntityUid uid, T component, GotEquippedEvent args)
        {
            if (!TryComp<ClothingComponent>(uid, out var clothing)) return;

            if (args.Equipee != _player.LocalPlayer?.ControlledEntity) return;

            if (!clothing.Slots.HasFlag(args.SlotFlags)) return;

            ApplyOverlay(component);
        }

        private void OnCompUnequip(EntityUid uid, T component, GotUnequippedEvent args)
        {
            if (args.Equipee != _player.LocalPlayer?.ControlledEntity) return;

            RemoveOverlay();
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            RemoveOverlay();
        }
    }
}

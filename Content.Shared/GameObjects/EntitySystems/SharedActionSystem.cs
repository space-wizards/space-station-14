using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// Evicts action states with expired cooldowns and watches for items being added to inventory
    /// to invoke IITemActionsEquipped, or removed from inventory to revoke
    /// their associated item actions.
    /// </summary>
    public class SharedActionSystem : EntitySystem
    {
        private const float CooldownCheckIntervalSeconds = 10;
        private float _timeSinceCooldownCheck;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UnequippedMessage>(OnUnequip);
            SubscribeLocalEvent<UnequippedHandMessage>(OnHandUnequip);
            SubscribeLocalEvent<EquippedMessage>(OnEquip);
            SubscribeLocalEvent<EquippedHandMessage>(OnHandEquip);
        }

        private void OnHandEquip(EquippedHandMessage ev)
        {
            if (!ItemActionsEquippedEventArgs.TryCreateFrom(ev, out var itemActionsEquippedArgs))
                return;

            foreach (var itemActionsEquipped in ev.Equipped.GetAllComponents<IItemActionsEquipped>())
            {
                itemActionsEquipped.ItemActionsEquipped(itemActionsEquippedArgs);
            }
        }

        private void OnEquip(EquippedMessage ev)
        {
            if (!ItemActionsEquippedEventArgs.TryCreateFrom(ev, out var itemActionsEquippedArgs))
                return;

            foreach (var itemActionsEquipped in ev.Equipped.GetAllComponents<IItemActionsEquipped>())
            {
                itemActionsEquipped.ItemActionsEquipped(itemActionsEquippedArgs);
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timeSinceCooldownCheck += frameTime;
            if (_timeSinceCooldownCheck < CooldownCheckIntervalSeconds) return;

            foreach (var comp in ComponentManager.EntityQuery<SharedActionsComponent>(false))
            {
                comp.ExpireCooldowns();
            }
            _timeSinceCooldownCheck -= CooldownCheckIntervalSeconds;
        }

        private void OnHandUnequip(UnequippedHandMessage ev)
        {
            if (ev.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
            {
                actionsComponent.Revoke(ev.Unequipped);
            }
        }

        private void OnUnequip(UnequippedMessage ev)
        {
            if (ev.User.TryGetComponent<SharedActionsComponent>(out var actionsComponent))
            {
                actionsComponent.Revoke(ev.Unequipped);
            }
        }
    }
}

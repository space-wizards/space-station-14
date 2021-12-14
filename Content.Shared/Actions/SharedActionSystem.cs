using Content.Shared.Actions.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Evicts action states with expired cooldowns.
    /// </summary>
    public class SharedActionSystem : EntitySystem
    {
        private const float CooldownCheckIntervalSeconds = 10;
        private float _timeSinceCooldownCheck;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ItemActionsComponent, GotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<ItemActionsComponent, GotUnequippedEvent>(OnGotUnequipped);
        }

        private void OnGotUnequipped(EntityUid uid, ItemActionsComponent component, GotUnequippedEvent args)
        {
            component.RevokeAllFromHolder();
            component.Holder = null;
            component.HolderActionsComponent = null;
            component.IsEquipped = false;
            component.InHand = null;
        }

        private void OnGotEquipped(EntityUid uid, ItemActionsComponent component, GotEquippedEvent args)
        {
            // this entity cannot be granted actions if no actions component
            if (!TryComp<SharedActionsComponent>(args.Equipee, out var actionsComponent))
                return;
            component.Holder = args.Equipee;
            component.HolderActionsComponent = actionsComponent;
            component.IsEquipped = true;
            component.InHand = null;
            component.GrantOrUpdateAllToHolder();
        }

        /// <summary>
        /// Toggle the action on / off
        /// </summary>
        public void Toggle(EntityUid uid, ItemActionType actionType, bool toggleOn, ItemActionsComponent? component = null)
        {
            if(!Resolve(uid, ref component))
                return;
            component.GrantOrUpdate(actionType, toggleOn: toggleOn);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            _timeSinceCooldownCheck += frameTime;
            if (_timeSinceCooldownCheck < CooldownCheckIntervalSeconds) return;

            foreach (var comp in EntityManager.EntityQuery<SharedActionsComponent>(false))
            {
                comp.ExpireCooldowns();
            }
            _timeSinceCooldownCheck -= CooldownCheckIntervalSeconds;
        }
    }
}

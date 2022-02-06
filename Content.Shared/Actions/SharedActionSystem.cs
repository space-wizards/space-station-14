using Content.Shared.Actions.Components;
using Content.Shared.Hands;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;
using System;

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

            UpdatesOutsidePrediction = true;
            SubscribeLocalEvent<ItemActionsComponent, GotEquippedEvent>(OnGotEquipped);
            SubscribeLocalEvent<ItemActionsComponent, EquippedHandEvent>(OnHandEquipped);
            SubscribeLocalEvent<ItemActionsComponent, UnequippedHandEvent>((uid, comp, _) => OnUnequipped(uid, comp));
            SubscribeLocalEvent<ItemActionsComponent, GotUnequippedEvent>((uid, comp, _) => OnUnequipped(uid, comp));
        }

        private void OnGotEquipped(EntityUid uid, ItemActionsComponent component, GotEquippedEvent args)
        {
            if (!TryComp(args.Equipee, out SharedActionsComponent? actionsComponent))
                return;

            component.Holder = args.Equipee;
            component.HolderActionsComponent = actionsComponent;
            component.IsEquipped = true;
            component.GrantOrUpdateAllToHolder();
        }

        private void OnHandEquipped(EntityUid uid, ItemActionsComponent component, EquippedHandEvent args)
        {
            if (!TryComp(args.User, out SharedActionsComponent? actionsComponent))
                return;

            component.Holder = args.User;
            component.HolderActionsComponent = actionsComponent;
            component.IsEquipped = true;
            component.InHand = args.Hand;
            component.GrantOrUpdateAllToHolder();
        }

        private void OnUnequipped(EntityUid uid, ItemActionsComponent component)
        {
            component.RevokeAllFromHolder();
            component.Holder = null;
            component.HolderActionsComponent = null;
            component.IsEquipped = false;
            component.InHand = null;
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

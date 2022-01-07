using Content.Shared.Actions.Components;
using Content.Shared.Hands;
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
            SubscribeLocalEvent<ItemActionsComponent, UnequippedHandEvent>(OnHandUnequipped);
            SubscribeLocalEvent<ItemActionsComponent, EquippedHandEvent>(OnHandEquipped);
        }

        private void OnHandEquipped(EntityUid uid, ItemActionsComponent component, EquippedHandEvent args)
        {
            component.EquippedHand(args.User, args.Hand);
        }

        private void OnHandUnequipped(EntityUid uid, ItemActionsComponent component, UnequippedHandEvent args)
        {
            component.UnequippedHand();
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

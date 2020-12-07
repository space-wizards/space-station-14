using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Shared.GameObjects.EntitySystems
{
    /// <summary>
    /// Evicts action states with expired cooldowns and watches for items being removed from inventory to revoke
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

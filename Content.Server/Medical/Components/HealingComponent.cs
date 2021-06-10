using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Stack;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Medical.Components
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract
    {
        public override string Name => "Healing";

        [DataField("heal")] public Dictionary<DamageType, int> Heal { get; private set; } = new();

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent? damageable))
            {
                return true;
            }

            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                return true;
            }

            if (eventArgs.User != eventArgs.Target &&
                !eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            {
                return true;
            }

            if (Owner.HasComponent<StackComponent>())
            {
                var stackUse = new StackUseEvent() {Amount = 1};
                Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, stackUse);

                if(!stackUse.Result)
                    return true;
            }

            foreach (var (type, amount) in Heal)
            {
                damageable.ChangeDamage(type, -amount, true);
            }

            return true;
        }
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Stack;
using Content.Shared.Damage;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Medical
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract
    {
        public override string Name => "Healing";

        public Dictionary<DamageType, int> Heal { get; private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, h => h.Heal, "heal", new Dictionary<DamageType, int>());
        }

        public async Task<bool> AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (eventArgs.Target == null)
            {
                return false;
            }

            if (!eventArgs.Target.TryGetComponent(out IDamageableComponent damageable))
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

            if (Owner.TryGetComponent(out StackComponent stack) &&
                !stack.Use(1))
            {
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

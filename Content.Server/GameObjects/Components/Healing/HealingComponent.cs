using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Healing
{
    [RegisterComponent]
    public class HealingComponent : Component, IAfterInteract, IUse
    {
        public override string Name => "Healing";

        public int Heal = 100;
        public DamageType Damage = DamageType.Brute;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Heal, "heal", 100);
            serializer.DataField(ref Damage, "damage", DamageType.Brute);
        }

        void IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!InteractionChecks.InRangeUnobstructed(eventArgs)) return;

            if (eventArgs.Target == null)
            {
                return;
            }

            if (!eventArgs.Target.TryGetComponent(out DamageableComponent damagecomponent)) return;
            if (Owner.TryGetComponent(out StackComponent stackComponent))
            {
                if (!stackComponent.Use(1))
                {
                    Owner.Delete();
                    return;
                }

                damagecomponent.TakeHealing(Damage, Heal);
                return;
            }
            damagecomponent.TakeHealing(Damage, Heal);
            Owner.Delete();
        }

        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            if (!eventArgs.User.TryGetComponent(out DamageableComponent damagecomponent)) return false;
            if (Owner.TryGetComponent(out StackComponent stackComponent))
            {
                if (!stackComponent.Use(1))
                {
                    Owner.Delete();
                    return false;
                }

                damagecomponent.TakeHealing(Damage, Heal);
                return false;
            }
            damagecomponent.TakeHealing(Damage, Heal);
            Owner.Delete();
            return false;
        }
    }
}

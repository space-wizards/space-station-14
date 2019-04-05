using System;
using Content.Server.GameObjects.Components.Stack;
using SS14.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using SS14.Shared.IoC;
using SS14.Server.GameObjects;
using SS14.Shared.Maths;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Serialization;
using SS14.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    public class HealingComponent : Component, IAfterAttack, IUse
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

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            if (eventArgs.Attacked == null)
            {
                return;
            }

            if (!eventArgs.Attacked.TryGetComponent(out DamageableComponent damagecomponent)) return;
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

        bool IUse.UseEntity(IEntity user)
        {
            if (!user.TryGetComponent(out DamageableComponent damagecomponent)) return false;
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

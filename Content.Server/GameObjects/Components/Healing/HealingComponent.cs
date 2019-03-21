using System;
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Heal, "heal", 100);
        }

        void IAfterAttack.Afterattack(IEntity user, GridCoordinates clicklocation, IEntity attacked)
        {
            if (attacked == null)
            {
                return;
            }
            if (attacked.TryGetComponent(out DamageableComponent damagecomponent))
            {
                damagecomponent.TakeHealing(DamageType.Brute, Heal);
                DropAndDelete(user);
            }
        }

        bool IUse.UseEntity(IEntity user)
        {
            if (user.TryGetComponent(out DamageableComponent damagecomponent))
            {
                damagecomponent.TakeHealing(DamageType.Brute, Heal);
                DropAndDelete(user);
                return false;
            }
            return false;
        }

        void DropAndDelete(IEntity user)
        {
            if(user.TryGetComponent(out HandsComponent handscomponent))
            {
                handscomponent.Drop(Owner);
                Owner.Delete();
            }
        }
    }
}

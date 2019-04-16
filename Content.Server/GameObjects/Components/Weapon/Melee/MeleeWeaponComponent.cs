using System;
using Robust.Shared.GameObjects;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.IoC;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.Serialization;
using Robust.Shared.Interfaces.GameObjects.Components;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    public class MeleeWeaponComponent : Component, IAfterAttack
    {
        public override string Name => "MeleeWeapon";

        public int Damage = 1;
        public float Range = 1;
        public float ArcWidth = 90;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref Damage, "damage", 5);
            serializer.DataField(ref Range, "range", 1);
            serializer.DataField(ref ArcWidth, "arcwidth", 90);
        }

        void IAfterAttack.AfterAttack(AfterAttackEventArgs eventArgs)
        {
            var location = eventArgs.User.GetComponent<ITransformComponent>().GridPosition;
            var angle = new Angle(eventArgs.ClickLocation.ToWorld().Position - location.ToWorld().Position);
            var entities = IoCManager.Resolve<IServerEntityManager>().GetEntitiesInArc(eventArgs.User.GetComponent<ITransformComponent>().GridPosition, Range, angle, ArcWidth);

            foreach (var entity in entities)
            {
                if (!entity.GetComponent<ITransformComponent>().IsMapTransform || entity == eventArgs.User)
                    continue;

                if (entity.TryGetComponent(out DamageableComponent damagecomponent))
                {
                    damagecomponent.TakeDamage(DamageType.Brute, Damage);
                }
            }
        }
    }
}

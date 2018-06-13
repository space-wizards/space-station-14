using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Shared.Audio;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Physics;
using System;

namespace Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan
{
    public class HitscanWeaponComponent : RangedWeaponComponent
    {
        public override string Name => "HitscanWeapon";

        string spritename = "Objects/laser.png";

        protected override void Fire(IEntity user, GridLocalCoordinates clicklocation)
        {
            var userposition = user.GetComponent<TransformComponent>().WorldPosition; //Remember world positions are ephemeral and can only be used instantaneously
            var angle = new Angle(clicklocation.Position - userposition);

            var ray = new Ray(userposition, angle.ToVec());
            var raycastresults = IoCManager.Resolve<ICollisionManager>().IntersectRay(ray, 20, Owner.GetComponent<TransformComponent>().GetMapTransform().Owner);

            Hit(raycastresults);
            AfterEffects(user, raycastresults, angle);
        }

        protected virtual void Hit(RayCastResults ray)
        {
            if (ray.HitEntity != null && ray.HitEntity.TryGetComponent(out DamageableComponent damage))
            {
                damage.TakeDamage(DamageType.Heat, 10);
            }
        }

        protected virtual void AfterEffects(IEntity user, RayCastResults ray, Angle angle)
        {
            var time = IoCManager.Resolve<IGameTiming>().CurTime;
            var offset = angle.ToVec() * ray.Distance / 2;

            EffectSystemMessage message = new EffectSystemMessage
            {
                EffectSprite = spritename,
                Born = time,
                DeathTime = time + TimeSpan.FromSeconds(1),
                Size = new Vector2(ray.Distance, 1f),
                Coordinates = user.GetComponent<TransformComponent>().LocalPosition.Translated(offset),
                //Rotated from east facing
                Rotation = (float)angle.Theta,
                ColorDelta = new Vector4(0, 0, 0, -1500f),
                Color = new Vector4(255, 255, 255, 750),
                Shaded = false
            };
            var mgr = IoCManager.Resolve<IEntitySystemManager>();
            mgr.GetEntitySystem<EffectSystem>().CreateParticle(message);
            mgr.GetEntitySystem<AudioSystem>().Play("/Audio/laser.ogg", Owner, AudioParams.Default.WithVolume(-5));
        }
    }
}

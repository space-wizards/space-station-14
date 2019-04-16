using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.GameObjects.Components;
using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;
using Robust.Shared.Utility;
using Content.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Projectiles
{
    public class ProjectileComponent : Component, ICollideSpecial, ICollideBehavior
    {
        public override string Name => "Projectile";

        public bool IgnoreShooter = true;

        private EntityUid Shooter = EntityUid.Invalid;

        public Dictionary<DamageType, int> damages = new Dictionary<DamageType, int>();

        /// <summary>
        /// Function that makes the collision of this object ignore a specific entity so we don't collide with ourselves
        /// </summary>
        /// <param name="shooter"></param>
        public void IgnoreEntity(IEntity shooter)
        {
            Shooter = shooter.Uid;
        }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(ICollidable collidedwith)
        {
            if (IgnoreShooter && collidedwith.Owner.Uid == Shooter)
                return true;
            return false;
        }

        /// <summary>
        /// Applys the damage when our projectile collides with its victim
        /// </summary>
        /// <param name="collidedwith"></param>
        void ICollideBehavior.CollideWith(List<IEntity> collidedwith)
        {
            foreach(var entity in collidedwith)
            {
                if(entity.TryGetComponent(out DamageableComponent damage))
                {
                    damage.TakeDamage(DamageType.Brute, 10);
                }
            }

            if (collidedwith.Count > 0)
                Owner.Delete();
        }
    }
}

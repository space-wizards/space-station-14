using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Weapon;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Upon colliding with an object this will flash in an area around it
    /// </summary>
    [RegisterComponent]
    public class FlashProjectileComponent : Component, ICollideBehavior, ICollideSpecial
    {
        public override string Name => "FlashProjectile";

        private double _range;
        private double _duration;
        private string _sound;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _range, "range", 1.0);
            serializer.DataField(ref _duration, "duration", 5.0);
            serializer.DataField(ref _sound, "sound", "/Audio/effects/snap.ogg");
        }

        public override void Initialize()
        {
            base.Initialize();
            // Shouldn't be using this without a ProjectileComponent because it will just immediately collide with thrower
            if (!Owner.HasComponent<ProjectileComponent>())
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Special collision override, can be used to give custom behaviors deciding when to collide
        /// </summary>
        /// <param name="collidedwith"></param>
        /// <returns></returns>
        bool ICollideSpecial.PreventCollide(IPhysBody collidedwith)
        {
            if (Owner.TryGetComponent(out ProjectileComponent projectileComponent))
            {
                if (projectileComponent.IgnoreShooter && collidedwith.Owner.Uid == projectileComponent.Shooter)
                {
                    return true;
                }
            }

            return false;
        }

        void ICollideBehavior.CollideWith(List<IEntity> collidedwith)
        {
            if (collidedwith.Count == 0)
            {
                return;
            }
            ServerFlashableComponent.FlashAreaHelper(Owner, _range, _duration, _sound);
        }
    }
}

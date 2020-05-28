using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.StatusEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Projectiles
{
    /// <summary>
    /// Adds stun when it collides with an entity
    /// </summary>
    [RegisterComponent]
    public sealed class StunProjectileComponent : Component, ICollideBehavior
    {
        public override string Name => "StunProjectile";

        private int _stunAmount;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _stunAmount, "stun_amount", 50);
        }

        public override void Initialize()
        {
            base.Initialize();
            if (!Owner.HasComponent<ProjectileComponent>())
            {
                Logger.Error("StunProjectile entity must have a ProjectileComponent");
                throw new InvalidOperationException();
            }
        }

        public void CollideWith(List<IEntity> collidedwith)
        {
            if (collidedwith.Count == 0)
            {
                return;
            }

            foreach (var entity in collidedwith)
            {
                if (entity.TryGetComponent(out StunnableComponent stunnableComponent))
                {
                    stunnableComponent.AddStun(_stunAmount);
                }
            }
        }
    }
}

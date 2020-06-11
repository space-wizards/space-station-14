using System;
using System.Linq;
using Content.Shared.GameObjects.Components.Weapons;
using Content.Shared.Physics;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Weapon
{
    [RegisterComponent]
    public sealed class ServerFlashableComponent : SharedFlashableComponent
    {
        private double _duration;
        private TimeSpan _lastFlash;

        public void Flash(double duration)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            _lastFlash = timing.CurTime;
            _duration = duration;
            Dirty();
        }

        public override ComponentState GetComponentState()
        {
            return new FlashComponentState(_duration, _lastFlash);
        }

        public static void FlashAreaHelper(IEntity source, double range, double duration, string sound = null)
        {
            var physicsManager = IoCManager.Resolve<IPhysicsManager>();
            var entityManager = IoCManager.Resolve<IEntityManager>();

            foreach (var entity in entityManager.GetEntities(new TypeEntityQuery(typeof(ServerFlashableComponent))))
            {
                if (source.Transform.MapID != entity.Transform.MapID ||
                entity == source)
                {
                    continue;
                }
                
                var direction = entity.Transform.WorldPosition - source.Transform.WorldPosition;

                if (direction.Length > range)
                {
                    continue;
                }

                // Direction will be zero if they're hit with the source only I think
                if (direction == Vector2.Zero)
                {
                    continue;
                }

                var ray = new CollisionRay(source.Transform.WorldPosition, direction.Normalized, (int) CollisionGroup.Opaque);
                var rayCastResults = physicsManager.IntersectRay(source.Transform.MapID, ray, direction.Length, source, false).ToList();
                if (rayCastResults.Count == 0 ||
                    rayCastResults[0].HitEntity != entity)
                {
                    continue;
                }

                var flashable = entity.GetComponent<ServerFlashableComponent>();
                flashable.Flash(duration);
            }

            if (sound != null)
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().PlayAtCoords(sound, source.Transform.GridPosition);
            }
        }
    }
}

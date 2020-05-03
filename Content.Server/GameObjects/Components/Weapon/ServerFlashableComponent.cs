using System;
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
            var componentManager = IoCManager.Resolve<IComponentManager>();

            foreach (var component in componentManager.GetAllComponents(typeof(ServerFlashableComponent)))
            {
                if (component.Owner.Transform.MapID != source.Transform.MapID ||
                    component.Owner == source ||
                    (component.Owner.Transform.WorldPosition - source.Transform.WorldPosition).Length >= range)
                {
                    continue;
                }

                var direction = component.Owner.Transform.WorldPosition - source.Transform.WorldPosition;
                // Direction will be zero if they're hit with the source only I think
                if (direction != Vector2.Zero)
                {
                    var ray = new CollisionRay(source.Transform.WorldPosition, direction.Normalized, (int) CollisionGroup.Opaque);
                    var rayResult = physicsManager.IntersectRay(component.Owner.Transform.MapID, ray, direction.Length, source);
                    // Doesn't matter whether the Flashable target blocks light or not
                    if (rayResult.DidHitObject)
                    {
                        continue;
                    }
                }

                // Doesn't matter whether the Flashable target blocks light or not
                var flashable = (ServerFlashableComponent) component;
                flashable.Flash(duration);

            }

            if (sound != null)
            {
                IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<AudioSystem>().Play(sound, source.Transform.GridPosition);
            }
        }
    }
}

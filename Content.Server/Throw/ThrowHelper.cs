using Content.Server.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Random;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.Throw
{
    public static class ThrowHelper
    {
        public static void Throw(IEntity thrownEnt, float throwForce, GridCoordinates targetLoc, GridCoordinates sourceLoc, bool spread = false, IEntity throwSourceEnt = null)
        {
            if (!thrownEnt.TryGetComponent(out CollidableComponent colComp))
                return;

            var mapManager = IoCManager.Resolve<IMapManager>();

            colComp.CollisionEnabled = true;
            // I can now collide with player, so that i can do damage.

            if (!thrownEnt.TryGetComponent(out ThrownItemComponent projComp))
            {
                projComp = thrownEnt.AddComponent<ThrownItemComponent>();

                if (colComp.PhysicsShapes.Count == 0)
                    colComp.PhysicsShapes.Add(new PhysShapeAabb());

                colComp.PhysicsShapes[0].CollisionMask |= (int) (CollisionGroup.MobImpassable | CollisionGroup.Impassable);
                colComp.IsScrapingFloor = false;
            }
            var angle = new Angle(targetLoc.ToMapPos(mapManager) - sourceLoc.ToMapPos(mapManager));

            if (spread)
            {
                var spreadRandom = IoCManager.Resolve<IRobustRandom>();
                angle += Angle.FromDegrees(spreadRandom.NextGaussian(0, 3));
            }

            if (throwSourceEnt != null)
            {
                projComp.User = throwSourceEnt;
                projComp.IgnoreEntity(throwSourceEnt);

                throwSourceEnt.Transform.LocalRotation = angle.GetCardinalDir().ToAngle();
            }

            if (!thrownEnt.TryGetComponent(out PhysicsComponent physComp))
                physComp = thrownEnt.AddComponent<PhysicsComponent>();

            // TODO: Move this into PhysicsSystem, we need an ApplyForce function.
            var a = throwForce / (float) Math.Max(0.001, physComp.Mass); // a = f / m

            var timing = IoCManager.Resolve<IGameTiming>();
            var spd = a / (1f / timing.TickRate); // acceleration is applied in 1 tick instead of 1 second, scale appropriately

            physComp.LinearVelocity = angle.ToVec() * spd;
        }
    }
}

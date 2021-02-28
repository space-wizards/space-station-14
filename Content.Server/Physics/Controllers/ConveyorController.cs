#nullable enable
using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Conveyor;
using Content.Server.GameObjects.Components.Recycling;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;

namespace Content.Server.Physics.Controllers
{
    internal sealed class ConveyorController : VirtualController
    {
        public override List<Type> UpdatesAfter => new() {typeof(MoverController)};

        public override void UpdateBeforeSolve(bool prediction, float frameTime)
        {
            base.UpdateBeforeSolve(prediction, frameTime);
            foreach (var comp in ComponentManager.EntityQuery<ConveyorComponent>())
            {
                Convey(comp, frameTime);
            }

            // TODO: Uhh you can probably wrap the recycler's conveying properties into... conveyor
            foreach (var comp in ComponentManager.EntityQuery<RecyclerComponent>())
            {
                ConveyRecycler(comp, frameTime);
            }
        }

        private void Convey(ConveyorComponent comp, float frameTime)
        {
            // TODO: Use ICollideBehavior and cache intersecting
            // Use an event for conveyors to know what needs to run
            if (!comp.CanRun())
            {
                return;
            }

            var intersecting = EntityManager.GetEntitiesIntersecting(comp.Owner, true);
            var direction = comp.GetAngle().ToVec();
            Vector2? ownerPos = null;

            foreach (var entity in intersecting)
            {
                if (!comp.CanMove(entity)) continue;

                if (!entity.TryGetComponent(out IPhysBody? physics) || physics.BodyStatus == BodyStatus.InAir ||
                    entity.IsWeightless()) continue;

                ownerPos ??= comp.Owner.Transform.WorldPosition;
                var itemRelativeToConveyor = entity.Transform.WorldPosition - ownerPos.Value;

                physics.LinearVelocity += Convey(direction * comp.Speed, frameTime, itemRelativeToConveyor);
            }
        }

        // TODO Uhhh I did a shit job plz fix smug
        private Vector2 Convey(Vector2 velocityDirection, float frameTime, Vector2 itemRelativeToConveyor)
        {
            //gravitating item towards center
            //http://csharphelper.com/blog/2016/09/find-the-shortest-distance-between-a-point-and-a-line-segment-in-c/
            Vector2 centerPoint;

            var t = 0f;
            if (velocityDirection.Length > 0) // if velocitydirection is 0, this calculation will divide by 0
            {
                t = Vector2.Dot(itemRelativeToConveyor, velocityDirection) /
                    Vector2.Dot(velocityDirection, velocityDirection);
            }

            if (t < 0)
            {
                centerPoint = new Vector2();
            }
            else if (t > 1)
            {
                centerPoint = velocityDirection;
            }
            else
            {
                centerPoint = velocityDirection * t;
            }

            var delta = centerPoint - itemRelativeToConveyor;
            return delta * (400 * delta.Length) * frameTime;
        }

        private void ConveyRecycler(RecyclerComponent comp, float frameTime)
        {
            if (!comp.CanRun())
            {
                comp.Intersecting.Clear();
                return;
            }

            var direction = Vector2.UnitX;
            Vector2? ownerPos = null;

            for (var i = comp.Intersecting.Count - 1; i >= 0; i--)
            {
                var entity = comp.Intersecting[i];

                if (entity.Deleted || !comp.CanMove(entity) || !EntityManager.IsIntersecting(comp.Owner, entity))
                {
                    comp.Intersecting.RemoveAt(i);
                    continue;
                }

                if (!entity.TryGetComponent(out IPhysBody? physics)) continue;
                ownerPos ??= comp.Owner.Transform.WorldPosition;
                physics.LinearVelocity += Convey(direction, frameTime, entity.Transform.WorldPosition - ownerPos.Value);
            }
        }
    }
}

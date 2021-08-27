using System;
using System.Collections.Generic;
using Content.Server.Conveyor;
using Content.Server.Recycling.Components;
using Content.Shared.Movement.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
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
            var system = EntitySystem.Get<ConveyorSystem>();
            foreach (var comp in ComponentManager.EntityQuery<ConveyorComponent>())
            {
                Convey(system, comp, frameTime);
            }

            // TODO: Uhh you can probably wrap the recycler's conveying properties into... conveyor
            foreach (var comp in ComponentManager.EntityQuery<RecyclerComponent>())
            {
                ConveyRecycler(comp, frameTime);
            }
        }

        private void Convey(ConveyorSystem system, ConveyorComponent comp, float frameTime)
        {
            // TODO: Use ICollideBehavior and cache intersecting
            // Use an event for conveyors to know what needs to run
            if (!system.CanRun(comp))
            {
                return;
            }

            var direction = system.GetAngle(comp).ToVec();
            var ownerPos = comp.Owner.Transform.WorldPosition;

            foreach (var (entity, physics) in EntitySystem.Get<ConveyorSystem>().GetEntitiesToMove(comp))
            {
                var itemRelativeToConveyor = entity.Transform.WorldPosition - ownerPos;
                physics.LinearVelocity += Convey(direction, comp.Speed, frameTime, itemRelativeToConveyor);
            }
        }

        private Vector2 Convey(Vector2 direction, float speed, float frameTime, Vector2 itemRelativeToConveyor)
        {
            if(speed == 0 || direction.Length == 0) return Vector2.Zero;
            direction = direction.Normalized;

            var dirNormal = new Vector2(direction.Y, direction.X);
            var dot = Vector2.Dot(itemRelativeToConveyor, dirNormal);

            var velocity = direction * speed * 5;
            velocity += dirNormal * speed * -dot;

            return velocity * frameTime;
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

            // TODO: I know it sucks but conveyors need a refactor
            for (var i = comp.Intersecting.Count - 1; i >= 0; i--)
            {
                var entity = comp.Intersecting[i];

                if (entity.Deleted || !comp.CanMove(entity) || !IoCManager.Resolve<IEntityLookup>().IsIntersecting(comp.Owner, entity))
                {
                    comp.Intersecting.RemoveAt(i);
                    continue;
                }

                if (!entity.TryGetComponent(out IPhysBody? physics)) continue;
                ownerPos ??= comp.Owner.Transform.WorldPosition;
                physics.LinearVelocity += Convey(direction, 1f, frameTime, entity.Transform.WorldPosition - ownerPos.Value);
            }
        }
    }
}
